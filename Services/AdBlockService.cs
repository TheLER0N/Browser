using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GhostBrowser.Models;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис блокировки рекламы и трекеров.
    /// Работает через WebResourceRequested API WebView2 —
    /// перехватывает запросы до их отправки и блокирует по правилам.
    /// 
    /// Аналог uBlock Origin, но встроенный в движок (WebView2 не поддерживает расширения Chrome).
    /// </summary>
    public class AdBlockService : IDisposable
    {
        private bool _isEnabled = true;
        private int _totalBlocked;
        private readonly string _filtersFile;
        private bool _isDisposed;

        /// <summary>
        /// Встроенные правила блокировки — базовый набор без загрузки извне.
        /// Вдохновлено EasyList, упрощённая версия.
        /// </summary>
        private static readonly List<string> DefaultAdPatterns = new()
        {
            // ═══ Рекламные сети ═══
            "doubleclick.net",
            "googleadservices.com",
            "googlesyndication.com",
            "adservice.google.",
            "pagead2.googlesyndication.com",
            "adservice.",
            "adsystem.",
            "amazon-adsystem.com",
            "ads.yahoo.com",
            "adnxs.com",
            "ads-twitter.com",
            "ads.linkedin.com",
            "facebook.com/tr/",
            "connect.facebook.net",

            // ═══ Трекеры ═══
            "analytics.google",
            "google-analytics.com",
            "tracking.",
            "track.",
            "pixel.",
            "beacon.",
            "statcounter.com",
            "hotjar.com",
            "crazyegg.com",

            // ═══ URL-паттерны рекламы ═══
            "/ads/",
            "/ad-",
            "/ad.",
            "/adserver/",
            "/adframe",
            "/adbanner",
            "/advertising/",
            "/banners/",
            "/banner-",
            "/banner.",
            "/sponsor",
            "/sponsored/",
            "/promo/",
            "/promotion/",

            // ═══ Pop-up / overlay ═══
            "/popup",
            "/popunder",
            "/overlay-ad",
            "/interstitial",

            // ═══ Видео-реклама ═══
            "/vast.",
            "/vpaid.",
            "ima3.js",

            // ═══ Виджеты и соцсети ═══
            "/social-widgets/",
            "platform.twitter.com/widgets",
            "apis.google.com/js/platform.js",
        };

        /// <summary>
        /// Все списки фильтров (можно добавлять/отключать).
        /// </summary>
        public List<AdBlockFilterList> FilterLists { get; } = new();

        /// <summary>
        /// Включена ли блокировка рекламы.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; SaveSettings(); }
        }

        /// <summary>
        /// Общее количество заблокированных запросов.
        /// </summary>
        public int TotalBlocked
        {
            get => _totalBlocked;
            set { _totalBlocked = value; }
        }

        /// <summary>
        /// Событие — заблокирован запрос (для UI-обновления).
        /// </summary>
        public event Action<int>? BlockedCountChanged;

        public AdBlockService()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GhostBrowser");
            Directory.CreateDirectory(appData);
            _filtersFile = Path.Combine(appData, "adblock-filters.json");

            LoadFilters();
            InitializeDefaultLists();
        }

        /// <summary>
        /// Инициализирует списки фильтров по умолчанию.
        /// </summary>
        private void InitializeDefaultLists()
        {
            // Встроенный список — GhostBrowser Basic Filter
            var basicList = new AdBlockFilterList
            {
                Name = "KING Basic Filter",
                Description = "Встроенный набор правил для блокировки рекламы и трекеров",
                IsEnabled = true,
                SourceUrl = "built-in",
                Rules = DefaultAdPatterns.Select(p => new AdBlockRule { Pattern = p }).ToList(),
                BlockedCount = 0
            };

            // Добавляем только если ещё нет
            if (!FilterLists.Any(f => f.Name == basicList.Name))
            {
                FilterLists.Add(basicList);
            }
        }

        /// <summary>
        /// Проверяет URL по всем активным фильтрам.
        /// Вызывается из WebResourceRequested обработчика.
        /// </summary>
        public bool ShouldBlockUrl(string url)
        {
            if (!_isEnabled) return false;
            if (string.IsNullOrEmpty(url)) return false;

            var lowerUrl = url.ToLowerInvariant();

            // Не блокируем ghost:// и about: страницы
            if (lowerUrl.StartsWith("ghost://") || lowerUrl.StartsWith("about:"))
                return false;

            foreach (var list in FilterLists)
            {
                if (list.ShouldBlock(lowerUrl))
                {
                    _totalBlocked++;
                    BlockedCountChanged?.Invoke(_totalBlocked);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Переключает указанный список фильтров.
        /// </summary>
        public void ToggleFilterList(string name)
        {
            var list = FilterLists.FirstOrDefault(f => f.Name == name);
            if (list != null)
            {
                list.IsEnabled = !list.IsEnabled;
                SaveFilters();
            }
        }

        /// <summary>
        /// Сбрасывает счётчик заблокированных запросов.
        /// </summary>
        public void ResetBlockedCount()
        {
            _totalBlocked = 0;
            foreach (var list in FilterLists)
                list.BlockedCount = 0;
            SaveFilters();
            BlockedCountChanged?.Invoke(0);
        }

        /// <summary>
        /// Сохраняет состояние фильтров.
        /// </summary>
        private void SaveSettings()
        {
            SaveFilters();
        }

        /// <summary>
        /// Сохраняет фильтры в JSON.
        /// </summary>
        private void SaveFilters()
        {
            try
            {
                // Сохраняем только метаданные (не правила — они встроенные)
                var data = new
                {
                    IsEnabled = _isEnabled,
                    TotalBlocked = _totalBlocked,
                    FilterStates = FilterLists.Select(f => new { f.Name, f.IsEnabled, f.BlockedCount }).ToList()
                };
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filtersFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdBlock SaveFilters error: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает фильтры из JSON.
        /// </summary>
        private void LoadFilters()
        {
            try
            {
                if (File.Exists(_filtersFile))
                {
                    var json = File.ReadAllText(_filtersFile);
                    var data = JsonSerializer.Deserialize<FilterData>(json);
                    if (data != null)
                    {
                        _isEnabled = data.IsEnabled;
                        _totalBlocked = data.TotalBlocked;
                        // FilterStates будут применены после инициализации
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdBlock LoadFilters error: {ex.Message}");
            }
        }

        /// <summary>
        /// Применяет сохранённые состояния фильтров после инициализации.
        /// </summary>
        public void ApplySavedStates()
        {
            try
            {
                if (File.Exists(_filtersFile))
                {
                    var json = File.ReadAllText(_filtersFile);
                    var data = JsonSerializer.Deserialize<FilterData>(json);
                    if (data?.FilterStates != null)
                    {
                        foreach (var state in data.FilterStates)
                        {
                            var filter = FilterLists.FirstOrDefault(f => f.Name == state.Name);
                            if (filter != null)
                            {
                                filter.IsEnabled = state.IsEnabled;
                                filter.BlockedCount = state.BlockedCount;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdBlock ApplySavedStates error: {ex.Message}");
            }
        }

        private class FilterData
        {
            public bool IsEnabled { get; set; } = true;
            public int TotalBlocked { get; set; }
            public List<FilterState>? FilterStates { get; set; }
        }

        private class FilterState
        {
            public string Name { get; set; } = "";
            public bool IsEnabled { get; set; }
            public int BlockedCount { get; set; }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            SaveFilters();
        }
    }
}
