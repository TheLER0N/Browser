using System;
using System.Collections.Generic;

namespace GhostBrowser.Models
{
    /// <summary>
    /// Модель расширенных настроек браузера.
    /// Сериализуется в JSON вместе с AppSettings.
    /// </summary>
    public class AdvancedSettings
    {
        // ═══ 1. Язык и локализация ═══
        /// <summary>Код языка интерфейса: "ru", "en"</summary>
        public string UILanguage { get; set; } = "ru";
        /// <summary>Формат даты: "DD.MM.YYYY", "MM/DD/YYYY"</summary>
        public string DateFormat { get; set; } = "DD.MM.YYYY";
        /// <summary>Формат времени: 24 или 12 часов</summary>
        public bool Use24HourClock { get; set; } = true;

        // ═══ 2. Внешний вид ═══
        /// <summary>Тема: "dark", "light", "system"</summary>
        public string Theme { get; set; } = "dark";
        /// <summary>Акцентный цвет: "blue", "purple", "green", "red", "custom"</summary>
        public string AccentColor { get; set; } = "blue";
        /// <summary>Кастомный HEX акцентного цвета (если AccentColor == "custom")</summary>
        public string CustomAccentColor { get; set; } = "#0078D4";
        /// <summary>Масштаб страницы по умолчанию (1.0 = 100%)</summary>
        public double DefaultZoomLevel { get; set; } = 1.0;
        /// <summary>Показывать панель закладок</summary>
        public bool ShowBookmarksBar { get; set; } = true;
        /// <summary>Показывать статус-бар</summary>
        public bool ShowStatusBar { get; set; } = true;
        /// <summary>Скруглённые углы вкладок</summary>
        public bool RoundedTabs { get; set; } = true;

        // ═══ 3. Производительность ═══
        /// <summary>Аппаратное ускорение WebView2</summary>
        public bool HardwareAcceleration { get; set; } = true;
        /// <summary>Лимит RAM в МБ (0 = без ограничений)</summary>
        public int MemoryLimitMB { get; set; } = 0;
        /// <summary>Автоочистка кэша при закрытии</summary>
        public bool AutoClearCacheOnExit { get; set; } = false;
        /// <summary>Количество потоков для загрузок (1-8)</summary>
        public int DownloadThreads { get; set; } = 3;
        /// <summary>Предзагрузка страниц (prefetch)</summary>
        public bool PagePrefetch { get; set; } = true;

        // ═══ 4. Безопасность и приватность ═══
        /// <summary>Блокировка JavaScript на конкретных сайтах (список доменов)</summary>
        public List<string> JavaScriptBlockedList { get; set; } = new();
        /// <summary>Интервал автоочистки истории: "never", "1h", "1d", "7d", "30d", "always"</summary>
        public string AutoClearHistoryInterval { get; set; } = "never";
        /// <summary>Мастер-пароль (хэш SHA-256, пустой = отключён)</summary>
        public string MasterPasswordHash { get; set; } = "";
        /// <summary>Предупреждения о подозрительных сайтах</summary>
        public bool PhishingWarnings { get; set; } = true;
        /// <summary>Блокировка всплывающих окон</summary>
        public bool PopupBlocker { get; set; } = true;
        /// <summary>Запрет уведомлений сайтов</summary>
        public bool BlockNotifications { get; set; } = false;
        /// <summary>Запрет геолокации</summary>
        public bool BlockGeolocation { get; set; } = false;
        /// <summary>Запрет доступа к камере/микрофону</summary>
        public bool BlockCameraMicrophone { get; set; } = false;

        // ═══ 5. Сеть и DNS ═══
        /// <summary>Режим обхода блокировок: "none", "doh_cloudflare", "doh_google", "proxy"</summary>
        public string BypassMode { get; set; } = "none";
        /// <summary>Тип прокси: "socks5", "http"</summary>
        public string ProxyType { get; set; } = "socks5";
        /// <summary>Адрес прокси-сервера</summary>
        public string ProxyServer { get; set; } = "";
        /// <summary>Порт прокси-сервера</summary>
        public int ProxyServerPort { get; set; } = 1080;
        /// <summary>Имя пользователя прокси</summary>
        public string ProxyUsername { get; set; } = "";
        /// <summary>Пароль прокси</summary>
        public string ProxyPassword { get; set; } = "";
        /// <summary>DNS over HTTPS (вкл/выкл) — устаревшее, используется BypassMode</summary>
        public bool DoHEnabled { get; set; } = false;
        /// <summary>DoH провайдер: "cloudflare", "google", "quad9"</summary>
        public string DoHProvider { get; set; } = "cloudflare";
        /// <summary>Режим прокси: "none", "system", "manual" — устаревшее, используется BypassMode</summary>
        public string ProxyMode { get; set; } = "none";
        /// <summary>Адрес прокси-сервера (если ProxyMode == "manual") — устаревшее</summary>
        public string ProxyAddress { get; set; } = "";
        /// <summary>Порт прокси-сервера — устаревшее</summary>
        public int ProxyPort { get; set; } = 8080;
        /// <summary>Таймаут подключения в секундах</summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        /// <summary>Максимальное количество подключений на хост</summary>
        public int MaxConnectionsPerHost { get; set; } = 6;

        // ═══ 6. Загрузки ═══
        /// <summary>Спрашивать куда сохранять каждую загрузку</summary>
        public bool AskForDownloadFolder { get; set; } = false;
        /// <summary>Открывать папку загрузок по завершении</summary>
        public bool OpenFolderOnDownloadComplete { get; set; } = false;
        /// <summary>Автоочистка завершённых загрузок из списка (часы, 0 = никогда)</summary>
        public int AutoClearDownloadsHours { get; set; } = 0;
        /// <summary>Максимальное количество одновременных загрузок (1-10)</summary>
        public int MaxConcurrentDownloads { get; set; } = 5;
        /// <summary>Предупреждение о подозрительных файлах (.exe, .bat, .msi)</summary>
        public bool WarnOnExecutableDownloads { get; set; } = true;

        // ═══ 7. При запуске ═══
        /// <summary>Действие при запуске: "newtab", "homepage", "lastsession", "customurls"</summary>
        public string StartupMode { get; set; } = "newtab";
        /// <summary>Список URL для режима "customurls"</summary>
        public List<string> StartupUrls { get; set; } = new();
        /// <summary>Восстанавливать сессию после краха</summary>
        public bool RestoreSessionOnCrash { get; set; } = true;
        /// <summary>Количество последних сессий для восстановления (1-5)</summary>
        public int MaxSavedSessions { get; set; } = 3;

        // ═══ 8. Поиск ═══
        /// <summary>Поисковые подсказки</summary>
        public bool SearchSuggestions { get; set; } = true;
        /// <summary>Открывать результаты поиска в новой вкладке</summary>
        public bool OpenSearchInNewTab { get; set; } = false;
        /// <summary>Кастомные поисковые шорткаты (ключ → URL с {query})</summary>
        public Dictionary<string, string> SearchShortcuts { get; set; } = new();

        // ═══ 9. Уведомления ═══
        /// <summary>Звук при завершении загрузки</summary>
        public bool DownloadCompleteSound { get; set; } = false;
        /// <summary>Всплывающее уведомление при завершении загрузки</summary>
        public bool DownloadCompleteNotification { get; set; } = true;

        // ═══ 10. Скриншоты ═══
        /// <summary>Формат скриншотов: "png", "jpeg"</summary>
        public string ScreenshotFormat { get; set; } = "png";
        /// <summary>Папка сохранения скриншотов (пустой = спрашивать каждый раз)</summary>
        public string ScreenshotFolder { get; set; } = "";
        /// <summary>Автоматическое имя файла (дата_время)</summary>
        public bool ScreenshotAutoName { get; set; } = true;

        // ═══ 11. Экспериментальные ═══
        /// <summary>Включить DevTools по F12</summary>
        public bool EnableDevTools { get; set; } = false;
        /// <summary>Кастомный User-Agent (пустой = стандартный)</summary>
        public string CustomUserAgent { get; set; } = "";
        /// <summary>Блокировка WebGL</summary>
        public bool BlockWebGL { get; set; } = false;
        /// <summary>Блокировка Canvas API</summary>
        public bool BlockCanvas { get; set; } = false;
        /// <summary>Режим «только текст» (без картинок)</summary>
        public bool TextOnlyMode { get; set; } = false;
        /// <summary>Автовоспроизведение медиа (видео/аудио)</summary>
        public bool AutoPlayMedia { get; set; } = true;
    }
}
