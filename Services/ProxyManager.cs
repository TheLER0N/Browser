using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Бесплатный прокси из публичных списков.
    /// </summary>
    public class ProxyEntry
    {
        public string Address { get; set; } = "";
        public int Port { get; set; }
        public string Type { get; set; } = "http";
        public string Country { get; set; } = "";
        public string CountryFlag { get; set; } = "";
        public int SpeedMs { get; set; } = 0;
        public bool IsWorking { get; set; } = false;
        public string? Error { get; set; }
        public DateTime LastChecked { get; set; }
    }

    /// <summary>
    /// Менеджер бесплатных прокси с автозагрузкой и автопроверкой.
    /// Загружает живые прокси из ProxyScrape API (обновляется каждые 10 мин).
    /// НЕ требует прав администратора.
    /// </summary>
    public static class ProxyManager
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static List<ProxyEntry> _cachedProxies = new();
        private static DateTime _lastFetch = DateTime.MinValue;

        /// <summary>
        /// API ProxyScrape — бесплатный, без ключей, обновляется каждые 10 мин.
        /// </summary>
        private static readonly string[] _apiUrls = new[]
        {
            "https://api.proxyscrape.com/v4/free-proxy-list/get?request=display_proxies&proxy_type=http&timeout=10000&country=all&ssl=all&anonymity=all",
            "https://api.proxyscrape.com/v4/free-proxy-list/get?request=display_proxies&proxy_type=socks5&timeout=10000&country=all&ssl=all&anonymity=all",
            "https://api.proxyscrape.com/v4/free-proxy-list/get?request=display_proxies&proxy_type=socks4&timeout=10000&country=all&ssl=all&anonymity=all",
        };

        /// <summary>
        /// Загружает свежие прокси из API.
        /// Кеширует на 5 минут чтобы не спамить API.
        /// </summary>
        public static async Task<List<ProxyEntry>> FetchProxiesAsync(CancellationToken ct = default)
        {
            // Если кеши свежие (< 5 мин) — возвращаем их
            if ((DateTime.Now - _lastFetch).TotalMinutes < 5 && _cachedProxies.Count > 0)
                return _cachedProxies;

            var allProxies = new List<ProxyEntry>();

            foreach (var apiUrl in _apiUrls)
            {
                try
                {
                    var response = await _httpClient.GetStringAsync(apiUrl, ct);

                    // Формат: "ip:port ip:port ip:port ..." (пробел-разделитель)
                    var parts = response.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;

                        var colonIndex = trimmed.LastIndexOf(':');
                        if (colonIndex < 1) continue;

                        var ip = trimmed.Substring(0, colonIndex);
                        if (!int.TryParse(trimmed.Substring(colonIndex + 1), out var port)) continue;

                        // Определяем тип из URL
                        var type = apiUrl.Contains("socks5") ? "socks5" :
                                   apiUrl.Contains("socks4") ? "socks4" : "http";

                        allProxies.Add(new ProxyEntry
                        {
                            Address = ip,
                            Port = port,
                            Type = type,
                            Country = "—",
                            CountryFlag = "🌐"
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Proxy fetch error ({apiUrl}): {ex.Message}");
                }
            }

            // Убираем дубликаты
            _cachedProxies = allProxies
                .GroupBy(p => $"{p.Address}:{p.Port}")
                .Select(g => g.First())
                .OrderBy(_ => Guid.NewGuid()) // Перемешиваем
                .Take(50) // Берём 50 случайных (чтобы не проверять 1000+)
                .ToList();

            _lastFetch = DateTime.Now;
            return _cachedProxies;
        }

        /// <summary>
        /// Проверяет один прокси через TCP-подключение.
        /// </summary>
        public static async Task<bool> TestProxyAsync(ProxyEntry proxy, CancellationToken ct = default)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(proxy.Address, proxy.Port);
                var timeoutTask = Task.Delay(8000, ct);

                var completed = await Task.WhenAny(connectTask, timeoutTask);
                sw.Stop();

                if (completed == connectTask && connectTask.IsCompletedSuccessfully)
                {
                    proxy.IsWorking = true;
                    proxy.SpeedMs = (int)sw.ElapsedMilliseconds;
                    proxy.LastChecked = DateTime.Now;
                    proxy.Error = null;
                    tcp.Close();
                    return true;
                }
                else
                {
                    proxy.IsWorking = false;
                    proxy.Error = "Таймаут";
                    return false;
                }
            }
            catch (TaskCanceledException)
            {
                proxy.IsWorking = false;
                proxy.Error = "Таймаут";
                return false;
            }
            catch (Exception ex)
            {
                proxy.IsWorking = false;
                proxy.Error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Проверяет первые N прокси параллельно (батчами по 10).
        /// </summary>
        public static async Task<List<ProxyEntry>> TestAllAsync(int maxConcurrent = 10, CancellationToken ct = default)
        {
            if (_cachedProxies.Count == 0)
                await FetchProxiesAsync(ct);

            var results = new List<ProxyEntry>();

            // Разбиваем на батчи по maxConcurrent
            for (int i = 0; i < _cachedProxies.Count; i += maxConcurrent)
            {
                if (ct.IsCancellationRequested) break;

                var batch = _cachedProxies.Skip(i).Take(maxConcurrent).ToList();
                var tasks = batch.Select(async proxy =>
                {
                    await TestProxyAsync(proxy, ct);
                    return proxy;
                });

                var batchResults = await Task.WhenAll(tasks);
                results.AddRange(batchResults);
            }

            // Сортировка: работающие первыми, затем по скорости
            return results
                .OrderBy(p => p.IsWorking ? 0 : 1)
                .ThenBy(p => p.IsWorking ? p.SpeedMs : int.MaxValue)
                .ToList();
        }

        /// <summary>
        /// Возвращает кешированные прокси без проверки.
        /// </summary>
        public static List<ProxyEntry> GetCached() => new(_cachedProxies);

        /// <summary>
        /// Количество загруженных прокси.
        /// </summary>
        public static int CachedCount => _cachedProxies.Count;

        /// <summary>
        /// Рекомендация.
        /// </summary>
        public static string GetRecommendation() =>
            "💡 Загружается ~50 прокси из ProxyScrape API (обновляется каждые 10 мин). " +
            "Нажмите «🔄 Проверить все» для проверки. Живые прокси живут 1-24 часа, " +
            "поэтому проверяйте каждый раз заново. Для стабильности — Psiphon.";
    }
}
