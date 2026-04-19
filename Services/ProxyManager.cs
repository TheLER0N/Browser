using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
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
        public int SpeedMs { get; set; }
        public bool IsWorking { get; set; }
        public string? Error { get; set; }
        public DateTime LastChecked { get; set; }
    }

    /// <summary>
    /// Менеджер бесплатных прокси с автозагрузкой и автопроверкой.
    /// Загружает живые прокси из ProxyScrape API.
    /// </summary>
    public static class ProxyManager
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static List<ProxyEntry> _cachedProxies = new();
        private static DateTime _lastFetch = DateTime.MinValue;

        /// <summary>
        /// Публичный API ProxyScrape.
        /// Поддерживаем только типы, которые приложение умеет применить дальше.
        /// </summary>
        private static readonly string[] _apiUrls =
        {
            "https://api.proxyscrape.com/v4/free-proxy-list/get?request=display_proxies&proxy_type=http&timeout=10000&country=all&ssl=all&anonymity=all",
            "https://api.proxyscrape.com/v4/free-proxy-list/get?request=display_proxies&proxy_type=socks5&timeout=10000&country=all&ssl=all&anonymity=all",
        };

        /// <summary>
        /// Загружает свежие прокси из API.
        /// Кеширует на 5 минут, чтобы не спамить API.
        /// </summary>
        public static async Task<List<ProxyEntry>> FetchProxiesAsync(CancellationToken ct = default)
        {
            if ((DateTime.Now - _lastFetch).TotalMinutes < 5 && _cachedProxies.Count > 0)
                return _cachedProxies;

            var allProxies = new List<ProxyEntry>();

            foreach (var apiUrl in _apiUrls)
            {
                try
                {
                    var response = await _httpClient.GetStringAsync(apiUrl, ct);
                    var parts = response.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();
                        if (string.IsNullOrEmpty(trimmed))
                            continue;

                        var colonIndex = trimmed.LastIndexOf(':');
                        if (colonIndex < 1)
                            continue;

                        var ip = trimmed[..colonIndex];
                        if (!int.TryParse(trimmed[(colonIndex + 1)..], out var port))
                            continue;

                        var type = apiUrl.Contains("socks5", StringComparison.OrdinalIgnoreCase) ? "socks5" : "http";

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

            _cachedProxies = allProxies
                .GroupBy(p => $"{p.Address}:{p.Port}:{p.Type}")
                .Select(g => g.First())
                .OrderBy(_ => Guid.NewGuid())
                .Take(50)
                .ToList();

            _lastFetch = DateTime.Now;
            return _cachedProxies;
        }

        /// <summary>
        /// Проверяет один прокси через минимальный протокольный handshake.
        /// </summary>
        public static async Task<bool> TestProxyAsync(ProxyEntry proxy, CancellationToken ct = default)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                using var tcp = new TcpClient();
                using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                connectCts.CancelAfter(TimeSpan.FromSeconds(8));
                await tcp.ConnectAsync(proxy.Address, proxy.Port, connectCts.Token);

                if (!tcp.Connected)
                {
                    proxy.IsWorking = false;
                    proxy.Error = "Не удалось установить TCP-соединение";
                    return false;
                }

                using var stream = tcp.GetStream();
                using var protocolCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                protocolCts.CancelAfter(TimeSpan.FromSeconds(8));

                var isWorking = proxy.Type switch
                {
                    "http" => await VerifyHttpProxyAsync(stream, protocolCts.Token),
                    "socks5" => await VerifySocks5ProxyAsync(stream, protocolCts.Token),
                    _ => false
                };

                sw.Stop();

                proxy.IsWorking = isWorking;
                proxy.LastChecked = DateTime.Now;
                proxy.SpeedMs = isWorking ? (int)sw.ElapsedMilliseconds : 0;
                proxy.Error = isWorking ? null : "Прокси не прошел проверку протокола";
                return isWorking;
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
        /// Проверяет первые N прокси параллельно батчами.
        /// </summary>
        public static async Task<List<ProxyEntry>> TestAllAsync(int maxConcurrent = 10, CancellationToken ct = default)
        {
            if (_cachedProxies.Count == 0)
                await FetchProxiesAsync(ct);

            var results = new List<ProxyEntry>();

            for (int i = 0; i < _cachedProxies.Count; i += maxConcurrent)
            {
                if (ct.IsCancellationRequested)
                    break;

                var batch = _cachedProxies.Skip(i).Take(maxConcurrent).ToList();
                var tasks = batch.Select(async proxy =>
                {
                    await TestProxyAsync(proxy, ct);
                    return proxy;
                });

                var batchResults = await Task.WhenAll(tasks);
                results.AddRange(batchResults);
            }

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
            "Нажмите «🔄 Проверить все» для протокольной проверки. Живые прокси быстро меняются, " +
            "поэтому перепроверяйте их перед применением.";

        private static async Task<bool> VerifyHttpProxyAsync(NetworkStream stream, CancellationToken ct)
        {
            const string host = "www.cloudflare.com";
            var request = $"CONNECT {host}:443 HTTP/1.1\r\nHost: {host}:443\r\nProxy-Connection: Keep-Alive\r\n\r\n";
            var requestBytes = Encoding.ASCII.GetBytes(request);

            await stream.WriteAsync(requestBytes, ct);
            await stream.FlushAsync(ct);

            using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var statusLine = await reader.ReadLineAsync().WaitAsync(ct);
            if (string.IsNullOrWhiteSpace(statusLine))
                return false;

            if (!statusLine.StartsWith("HTTP/1.1 2", StringComparison.OrdinalIgnoreCase) &&
                !statusLine.StartsWith("HTTP/1.0 2", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string? line;
            do
            {
                line = await reader.ReadLineAsync().WaitAsync(ct);
            } while (line is { Length: > 0 });

            return true;
        }

        private static async Task<bool> VerifySocks5ProxyAsync(NetworkStream stream, CancellationToken ct)
        {
            byte[] greeting = new byte[] { 0x05, 0x01, 0x00 };
            await stream.WriteAsync(greeting, ct);
            await stream.FlushAsync(ct);

            byte[] response = ArrayPool<byte>.Shared.Rent(2);
            try
            {
                await ReadExactlyAsync(stream, response.AsMemory(0, 2), ct);
                return response[0] == 0x05 && response[1] == 0x00;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(response);
            }
        }

        private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer[offset..], ct);
                if (read == 0)
                    throw new IOException("Удаленная сторона закрыла соединение");

                offset += read;
            }
        }
    }
}
