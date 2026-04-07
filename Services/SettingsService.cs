using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GhostBrowser.ViewModels;

namespace GhostBrowser.Services
{
    public class DnsPreset
    {
        public string Name { get; set; } = "";
        public string Primary { get; set; } = "";
        public string Secondary { get; set; } = "";
    }

    public class AppSettings
    {
        public bool UseCustomDns { get; set; }
        public string CustomDns { get; set; } = "";
        public string SelectedDnsPreset { get; set; } = "Google";
        public bool DarkTheme { get; set; } = true;
        public double FontSize { get; set; } = 14;
        public string HomePage { get; set; } = "ghost://newtab";
        public string DefaultSearchEngine { get; set; } = "Google";
        public bool BlockTrackers { get; set; } = true;
        public bool BlockThirdPartyCookies { get; set; } = false;
        /// <summary>Папка для сохранения загруженных файлов.</summary>
        public string DownloadFolder { get; set; } = "";
        /// <summary>Расширенные настройки браузера.</summary>
        public Models.AdvancedSettings AdvancedSettings { get; set; } = new();
    }

    /// <summary>
    /// Сервис настроек приложения.
    /// Реализует INotifyPropertyChanged (архитектурная проблема — будет вынесено в SettingsViewModel в Фазе 3).
    /// Реализует IDisposable для корректного освобождения HttpClient.
    /// </summary>
    public class SettingsService : INotifyPropertyChanged, IDisposable
    {
        private AppSettings _settings = new();
        private readonly string _settingsFile;
        private readonly HttpClient _httpClient;
        private bool _isTestingDns;
        private string _dnsTestResult = "";
        private string _saveNotification = "";
        private bool _isDisposed;

        public static List<DnsPreset> DnsPresets { get; } = new()
        {
            new DnsPreset { Name = "Google", Primary = "8.8.8.8", Secondary = "8.8.4.4" },
            new DnsPreset { Name = "Cloudflare", Primary = "1.1.1.1", Secondary = "1.0.0.1" },
            new DnsPreset { Name = "OpenDNS", Primary = "208.67.222.222", Secondary = "208.67.220.220" },
            new DnsPreset { Name = "Quad9", Primary = "9.9.9.9", Secondary = "149.112.112.112" },
            new DnsPreset { Name = "AdGuard", Primary = "94.140.14.14", Secondary = "94.140.15.15" },
            new DnsPreset { Name = "UncensoredDNS (Дания)", Primary = "89.233.43.71", Secondary = "" },
            new DnsPreset { Name = "Digitale-Gesellschaft (RU)", Primary = "185.95.218.42", Secondary = "185.95.218.43" },
            new DnsPreset { Name = "AppliedPrivacy (Люксембург)", Primary = "85.214.7.22", Secondary = "" },
            new DnsPreset { Name = "BlahDNS (Финляндия)", Primary = "185.194.244.57", Secondary = "" },
            new DnsPreset { Name = "Control D (Anti-block)", Primary = "76.76.2.0", Secondary = "76.76.10.0" },
            new DnsPreset { Name = "NextDNS (Free tier)", Primary = "45.90.28.0", Secondary = "45.90.30.0" },
            new DnsPreset { Name = "Yandex (базовый)", Primary = "77.88.8.8", Secondary = "77.88.8.1" },
            new DnsPreset { Name = "Яндекс DNS (безопасный)", Primary = "77.88.8.88", Secondary = "77.88.8.2" },
            new DnsPreset { Name = "xbox-dns.ru", Primary = "31.192.108.181", Secondary = "" },
        };

        public ObservableCollection<DnsPreset> DnsPresetsSource { get; } = new();
        public ICommand TestDnsCommand { get; }

        public SettingsService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GhostBrowser");
            Directory.CreateDirectory(appData);
            _settingsFile = Path.Combine(appData, "settings.json");
            LoadSettings();
            foreach (var p in DnsPresets) DnsPresetsSource.Add(p);
            TestDnsCommand = new RelayCommand(_ => TestDns(), _ => !string.IsNullOrEmpty(CustomDns));
        }

        public AppSettings Settings { get => _settings; set { _settings = value; OnPropertyChanged(); } }
        public bool UseCustomDns { get => _settings.UseCustomDns; set { if (_settings.UseCustomDns != value) { _settings.UseCustomDns = value; OnPropertyChanged(); SaveSettings(); } } }
        public string CustomDns { get => _settings.CustomDns; set { if (_settings.CustomDns != value) { _settings.CustomDns = value; OnPropertyChanged(); } } }
        public string SelectedDnsPreset { get => _settings.SelectedDnsPreset; set { if (_settings.SelectedDnsPreset != value) { _settings.SelectedDnsPreset = value; ApplyDnsPreset(value); OnPropertyChanged(); } } }
        public bool DarkTheme { get => _settings.DarkTheme; set { if (_settings.DarkTheme != value) { _settings.DarkTheme = value; OnPropertyChanged(); SaveSettings(); } } }
        public double FontSize { get => _settings.FontSize; set { if (_settings.FontSize != value) { _settings.FontSize = value; OnPropertyChanged(); SaveSettings(); } } }
        public string HomePage { get => _settings.HomePage; set { if (_settings.HomePage != value) { _settings.HomePage = value; OnPropertyChanged(); } } }
        public string DefaultSearchEngine { get => _settings.DefaultSearchEngine; set { if (_settings.DefaultSearchEngine != value) { _settings.DefaultSearchEngine = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool BlockTrackers { get => _settings.BlockTrackers; set { if (_settings.BlockTrackers != value) { _settings.BlockTrackers = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool BlockThirdPartyCookies { get => _settings.BlockThirdPartyCookies; set { if (_settings.BlockThirdPartyCookies != value) { _settings.BlockThirdPartyCookies = value; OnPropertyChanged(); SaveSettings(); } } }

        /// <summary>
        /// Расширенные настройки браузера.
        /// Доступ через свойство для удобства.
        /// </summary>
        public Models.AdvancedSettings AdvancedSettings
        {
            get => _settings.AdvancedSettings;
            set { if (_settings.AdvancedSettings != value) { _settings.AdvancedSettings = value; OnPropertyChanged(); SaveSettings(); } }
        }

        // ═══ INPC-обёртки для часто используемых AdvancedSettings ═══

        public string UILanguage { get => _settings.AdvancedSettings.UILanguage; set { if (_settings.AdvancedSettings.UILanguage != value) { _settings.AdvancedSettings.UILanguage = value; OnPropertyChanged(); SaveSettings(); } } }
        public string Theme { get => _settings.AdvancedSettings.Theme; set { if (_settings.AdvancedSettings.Theme != value) { _settings.AdvancedSettings.Theme = value; OnPropertyChanged(); SaveSettings(); } } }
        public string AccentColor { get => _settings.AdvancedSettings.AccentColor; set { if (_settings.AdvancedSettings.AccentColor != value) { _settings.AdvancedSettings.AccentColor = value; OnPropertyChanged(); SaveSettings(); } } }
        public string CustomAccentColor { get => _settings.AdvancedSettings.CustomAccentColor; set { if (_settings.AdvancedSettings.CustomAccentColor != value) { _settings.AdvancedSettings.CustomAccentColor = value; OnPropertyChanged(); SaveSettings(); } } }
        public double DefaultZoomLevel { get => _settings.AdvancedSettings.DefaultZoomLevel; set { if (_settings.AdvancedSettings.DefaultZoomLevel != value) { _settings.AdvancedSettings.DefaultZoomLevel = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool ShowBookmarksBar { get => _settings.AdvancedSettings.ShowBookmarksBar; set { if (_settings.AdvancedSettings.ShowBookmarksBar != value) { _settings.AdvancedSettings.ShowBookmarksBar = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool ShowStatusBar { get => _settings.AdvancedSettings.ShowStatusBar; set { if (_settings.AdvancedSettings.ShowStatusBar != value) { _settings.AdvancedSettings.ShowStatusBar = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool RoundedTabs { get => _settings.AdvancedSettings.RoundedTabs; set { if (_settings.AdvancedSettings.RoundedTabs != value) { _settings.AdvancedSettings.RoundedTabs = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool HardwareAcceleration { get => _settings.AdvancedSettings.HardwareAcceleration; set { if (_settings.AdvancedSettings.HardwareAcceleration != value) { _settings.AdvancedSettings.HardwareAcceleration = value; OnPropertyChanged(); SaveSettings(); } } }
        public int MemoryLimitMB { get => _settings.AdvancedSettings.MemoryLimitMB; set { if (_settings.AdvancedSettings.MemoryLimitMB != value) { _settings.AdvancedSettings.MemoryLimitMB = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool AutoClearCacheOnExit { get => _settings.AdvancedSettings.AutoClearCacheOnExit; set { if (_settings.AdvancedSettings.AutoClearCacheOnExit != value) { _settings.AdvancedSettings.AutoClearCacheOnExit = value; OnPropertyChanged(); SaveSettings(); } } }
        public int DownloadThreads { get => _settings.AdvancedSettings.DownloadThreads; set { if (_settings.AdvancedSettings.DownloadThreads != value) { _settings.AdvancedSettings.DownloadThreads = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool PagePrefetch { get => _settings.AdvancedSettings.PagePrefetch; set { if (_settings.AdvancedSettings.PagePrefetch != value) { _settings.AdvancedSettings.PagePrefetch = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool PopupBlocker { get => _settings.AdvancedSettings.PopupBlocker; set { if (_settings.AdvancedSettings.PopupBlocker != value) { _settings.AdvancedSettings.PopupBlocker = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool BlockNotifications { get => _settings.AdvancedSettings.BlockNotifications; set { if (_settings.AdvancedSettings.BlockNotifications != value) { _settings.AdvancedSettings.BlockNotifications = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool BlockGeolocation { get => _settings.AdvancedSettings.BlockGeolocation; set { if (_settings.AdvancedSettings.BlockGeolocation != value) { _settings.AdvancedSettings.BlockGeolocation = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool BlockCameraMicrophone { get => _settings.AdvancedSettings.BlockCameraMicrophone; set { if (_settings.AdvancedSettings.BlockCameraMicrophone != value) { _settings.AdvancedSettings.BlockCameraMicrophone = value; OnPropertyChanged(); SaveSettings(); } } }
        public string BypassMode { get => _settings.AdvancedSettings.BypassMode; set { if (_settings.AdvancedSettings.BypassMode != value) { _settings.AdvancedSettings.BypassMode = value; OnPropertyChanged(); SaveSettings(); } } }
        public string ProxyType { get => _settings.AdvancedSettings.ProxyType; set { if (_settings.AdvancedSettings.ProxyType != value) { _settings.AdvancedSettings.ProxyType = value; OnPropertyChanged(); SaveSettings(); } } }
        public string ProxyServer { get => _settings.AdvancedSettings.ProxyServer; set { if (_settings.AdvancedSettings.ProxyServer != value) { _settings.AdvancedSettings.ProxyServer = value; OnPropertyChanged(); SaveSettings(); } } }
        public int ProxyServerPort { get => _settings.AdvancedSettings.ProxyServerPort; set { if (_settings.AdvancedSettings.ProxyServerPort != value) { _settings.AdvancedSettings.ProxyServerPort = value; OnPropertyChanged(); SaveSettings(); } } }
        public string ProxyUsername { get => _settings.AdvancedSettings.ProxyUsername; set { if (_settings.AdvancedSettings.ProxyUsername != value) { _settings.AdvancedSettings.ProxyUsername = value; OnPropertyChanged(); SaveSettings(); } } }
        public string ProxyPassword { get => _settings.AdvancedSettings.ProxyPassword; set { if (_settings.AdvancedSettings.ProxyPassword != value) { _settings.AdvancedSettings.ProxyPassword = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool DoHEnabled { get => _settings.AdvancedSettings.DoHEnabled; set { if (_settings.AdvancedSettings.DoHEnabled != value) { _settings.AdvancedSettings.DoHEnabled = value; OnPropertyChanged(); SaveSettings(); } } }
        public string DoHProvider { get => _settings.AdvancedSettings.DoHProvider; set { if (_settings.AdvancedSettings.DoHProvider != value) { _settings.AdvancedSettings.DoHProvider = value; OnPropertyChanged(); SaveSettings(); } } }
        public string ProxyMode { get => _settings.AdvancedSettings.ProxyMode; set { if (_settings.AdvancedSettings.ProxyMode != value) { _settings.AdvancedSettings.ProxyMode = value; OnPropertyChanged(); SaveSettings(); } } }
        public string ProxyAddress { get => _settings.AdvancedSettings.ProxyAddress; set { if (_settings.AdvancedSettings.ProxyAddress != value) { _settings.AdvancedSettings.ProxyAddress = value; OnPropertyChanged(); SaveSettings(); } } }
        public int ProxyPort { get => _settings.AdvancedSettings.ProxyPort; set { if (_settings.AdvancedSettings.ProxyPort != value) { _settings.AdvancedSettings.ProxyPort = value; OnPropertyChanged(); SaveSettings(); } } }
        public int ConnectionTimeoutSeconds { get => _settings.AdvancedSettings.ConnectionTimeoutSeconds; set { if (_settings.AdvancedSettings.ConnectionTimeoutSeconds != value) { _settings.AdvancedSettings.ConnectionTimeoutSeconds = value; OnPropertyChanged(); SaveSettings(); } } }
        public int MaxConnectionsPerHost { get => _settings.AdvancedSettings.MaxConnectionsPerHost; set { if (_settings.AdvancedSettings.MaxConnectionsPerHost != value) { _settings.AdvancedSettings.MaxConnectionsPerHost = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool AskForDownloadFolder { get => _settings.AdvancedSettings.AskForDownloadFolder; set { if (_settings.AdvancedSettings.AskForDownloadFolder != value) { _settings.AdvancedSettings.AskForDownloadFolder = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool OpenFolderOnDownloadComplete { get => _settings.AdvancedSettings.OpenFolderOnDownloadComplete; set { if (_settings.AdvancedSettings.OpenFolderOnDownloadComplete != value) { _settings.AdvancedSettings.OpenFolderOnDownloadComplete = value; OnPropertyChanged(); SaveSettings(); } } }
        public int MaxConcurrentDownloads { get => _settings.AdvancedSettings.MaxConcurrentDownloads; set { if (_settings.AdvancedSettings.MaxConcurrentDownloads != value) { _settings.AdvancedSettings.MaxConcurrentDownloads = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool WarnOnExecutableDownloads { get => _settings.AdvancedSettings.WarnOnExecutableDownloads; set { if (_settings.AdvancedSettings.WarnOnExecutableDownloads != value) { _settings.AdvancedSettings.WarnOnExecutableDownloads = value; OnPropertyChanged(); SaveSettings(); } } }
        public string StartupMode { get => _settings.AdvancedSettings.StartupMode; set { if (_settings.AdvancedSettings.StartupMode != value) { _settings.AdvancedSettings.StartupMode = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool RestoreSessionOnCrash { get => _settings.AdvancedSettings.RestoreSessionOnCrash; set { if (_settings.AdvancedSettings.RestoreSessionOnCrash != value) { _settings.AdvancedSettings.RestoreSessionOnCrash = value; OnPropertyChanged(); SaveSettings(); } } }
        public int MaxSavedSessions { get => _settings.AdvancedSettings.MaxSavedSessions; set { if (_settings.AdvancedSettings.MaxSavedSessions != value) { _settings.AdvancedSettings.MaxSavedSessions = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool SearchSuggestions { get => _settings.AdvancedSettings.SearchSuggestions; set { if (_settings.AdvancedSettings.SearchSuggestions != value) { _settings.AdvancedSettings.SearchSuggestions = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool OpenSearchInNewTab { get => _settings.AdvancedSettings.OpenSearchInNewTab; set { if (_settings.AdvancedSettings.OpenSearchInNewTab != value) { _settings.AdvancedSettings.OpenSearchInNewTab = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool DownloadCompleteSound { get => _settings.AdvancedSettings.DownloadCompleteSound; set { if (_settings.AdvancedSettings.DownloadCompleteSound != value) { _settings.AdvancedSettings.DownloadCompleteSound = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool DownloadCompleteNotification { get => _settings.AdvancedSettings.DownloadCompleteNotification; set { if (_settings.AdvancedSettings.DownloadCompleteNotification != value) { _settings.AdvancedSettings.DownloadCompleteNotification = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool EnableDevTools { get => _settings.AdvancedSettings.EnableDevTools; set { if (_settings.AdvancedSettings.EnableDevTools != value) { _settings.AdvancedSettings.EnableDevTools = value; OnPropertyChanged(); SaveSettings(); } } }
        public string CustomUserAgent { get => _settings.AdvancedSettings.CustomUserAgent; set { if (_settings.AdvancedSettings.CustomUserAgent != value) { _settings.AdvancedSettings.CustomUserAgent = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool BlockWebGL { get => _settings.AdvancedSettings.BlockWebGL; set { if (_settings.AdvancedSettings.BlockWebGL != value) { _settings.AdvancedSettings.BlockWebGL = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool BlockCanvas { get => _settings.AdvancedSettings.BlockCanvas; set { if (_settings.AdvancedSettings.BlockCanvas != value) { _settings.AdvancedSettings.BlockCanvas = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool TextOnlyMode { get => _settings.AdvancedSettings.TextOnlyMode; set { if (_settings.AdvancedSettings.TextOnlyMode != value) { _settings.AdvancedSettings.TextOnlyMode = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool AutoPlayMedia { get => _settings.AdvancedSettings.AutoPlayMedia; set { if (_settings.AdvancedSettings.AutoPlayMedia != value) { _settings.AdvancedSettings.AutoPlayMedia = value; OnPropertyChanged(); SaveSettings(); } } }

        // ═══ Скриншоты ═══
        public string ScreenshotFormat { get => _settings.AdvancedSettings.ScreenshotFormat; set { if (_settings.AdvancedSettings.ScreenshotFormat != value) { _settings.AdvancedSettings.ScreenshotFormat = value; OnPropertyChanged(); SaveSettings(); } } }
        public string ScreenshotFolder { get => _settings.AdvancedSettings.ScreenshotFolder; set { if (_settings.AdvancedSettings.ScreenshotFolder != value) { _settings.AdvancedSettings.ScreenshotFolder = value; OnPropertyChanged(); SaveSettings(); } } }
        public bool ScreenshotAutoName { get => _settings.AdvancedSettings.ScreenshotAutoName; set { if (_settings.AdvancedSettings.ScreenshotAutoName != value) { _settings.AdvancedSettings.ScreenshotAutoName = value; OnPropertyChanged(); SaveSettings(); } } }

        /// <summary>
        /// Папка загрузок по умолчанию.
        /// Если пуста — возвращает %USERPROFILE%\Downloads.
        /// </summary>
        public string DownloadFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_settings.DownloadFolder))
                {
                    // Дефолтная папка: %USERPROFILE%\Downloads
                    _settings.DownloadFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                }
                return _settings.DownloadFolder;
            }
            set
            {
                if (_settings.DownloadFolder != value)
                {
                    _settings.DownloadFolder = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }
        
        public bool IsTestingDns { get => _isTestingDns; set { _isTestingDns = value; OnPropertyChanged(); } }
        public string DnsTestResult { get => _dnsTestResult; set { _dnsTestResult = value; OnPropertyChanged(); } }
        public string SaveNotification { get => _saveNotification; set { _saveNotification = value; OnPropertyChanged(); } }

        private void ApplyDnsPreset(string name) { var p = DnsPresets.Find(x => x.Name == name); if (p != null) { CustomDns = p.Primary; SaveSettings(); } }

        /// <summary>Основной метод теста — вызывается ТОЛЬКО из Task.Run (фоновый поток).</summary>
        public async Task<List<string>> RunDnsTestAsync(string dns)
        {
            var results = new List<string>();
            var sites = new[]
            {
                ("Google", "https://www.google.com"),
                ("Gmail", "https://mail.google.com"),
                ("Cloudflare", "https://www.cloudflare.com"),
                ("Gemini", "https://gemini.google.com"),
                ("YouTube", "https://www.youtube.com"),
            };

            foreach (var (name, url) in sites)
            {
                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var resp = await _httpClient.GetAsync(url);
                    sw.Stop();
                    results.Add(resp.IsSuccessStatusCode ? $"🟢 {name} — {sw.ElapsedMilliseconds}мс" : $"🟡 {name} — {(int)resp.StatusCode}");
                }
                catch (HttpRequestException) { results.Add($"🔴 {name} — заблокирован"); }
                catch (TaskCanceledException) { results.Add($"🔴 {name} — таймаут"); }
                catch (Exception ex) { results.Add($"🔴 {name} — {ex.GetType().Name}"); }
            }

            if (!string.IsNullOrWhiteSpace(dns) && IsValidIp(dns))
            {
                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    using var udp = new UdpClient();
                    udp.Client.ReceiveTimeout = 5000;
                    udp.Connect(IPAddress.Parse(dns), 53);
                    byte[] query = { 0xAA,0xAA,0x01,0x00,0x00,0x01,0x00,0x00,0x00,0x00,0x00,0x00, 0x06,0x67,0x6F,0x6F,0x67,0x6C,0x65,0x03,0x63,0x6F,0x6D,0x00, 0x00,0x01,0x00,0x01 };
                    await udp.SendAsync(query, query.Length);
                    var rcv = udp.ReceiveAsync();
                    var tmo = Task.Delay(5000);
                    var done = await Task.WhenAny(rcv, tmo);
                    sw.Stop();
                    if (done == rcv) { var r = await rcv; results.Add(r.Buffer.Length > 0 ? $"🟢 DNS {dns} — {sw.ElapsedMilliseconds}мс" : $"🔴 DNS {dns} — пустой ответ"); }
                    else results.Add($"🔴 DNS {dns} — таймаут");
                }
                catch (Exception ex) { results.Add($"🔴 DNS {dns} — {ex.GetType().Name}"); }
            }

            var blocked = results.Any(r => r.StartsWith("🔴"));
            results.Add("");
            results.Add(blocked ? "⚠️ Некоторые сайты заблокированы" : "✅ Все сайты доступны");
            return results;
        }

        /// <summary>Устаревший — для обратной совместимости.</summary>
        public async Task<bool> TestDnsAsync(string dns)
        {
            if (string.IsNullOrWhiteSpace(dns)) { DnsTestResult = "⚠️ Введите адрес DNS"; return false; }
            if (!IsValidIp(dns)) { DnsTestResult = $"❌ Некорректный IP: {dns}"; return false; }
            IsTestingDns = true;
            DnsTestResult = "⏳ Проверка...";
            var results = await RunDnsTestAsync(dns);
            DnsTestResult = string.Join("\n", results);
            IsTestingDns = false;
            return !results.Any(r => r.StartsWith("🔴"));
        }

        /// <summary>
        /// Запускает DNS-тест в фоновом потоке.
        /// Обёрнут в try-catch с выводом результата пользователю.
        /// </summary>
        private async void TestDns()
        {
            try
            {
                await Task.Run(async () => { await TestDnsAsync(CustomDns); });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TestDns error: {ex.Message}");
                DnsTestResult = $"❌ Ошибка теста: {ex.Message}";
            }
        }

        public static bool IsValidIp(string ip) => IPAddress.TryParse(ip, out _);

        /// <summary>
        /// Сохраняет настройки в JSON-файл.
        /// Уведомление автоматически очищается через 3 секунды (DispatcherTimer, не ContinueWith).
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFile, json);
                SaveNotification = "✅ Настройки сохранены";

                // Используем DispatcherTimer вместо ContinueWith — безопасно для UI-потока
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    if (!_isDisposed) SaveNotification = "";
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettings error: {ex.Message}");
                SaveNotification = $"❌ {ex.Message}";
            }
        }

        /// <summary>
        /// Загружает настройки из JSON. Если файл повреждён — использует дефолтные настройки.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    var json = File.ReadAllText(_settingsFile);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                        _settings = loaded;
                }
            }
            catch (Exception ex)
            {
                // Файл настроек повреждён — используем дефолтные настройки
                System.Diagnostics.Debug.WriteLine($"LoadSettings error (using defaults): {ex.Message}");
                _settings = new AppSettings();
            }
        }

        public void ResetToDefaults() { _settings = new AppSettings(); OnPropertyChanged(); SaveSettings(); }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        /// <summary>
        /// Освобождает ресурсы — DISPOSит HttpClient.
        /// Вызывается из MainViewModel.Cleanup() при закрытии окна.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _httpClient?.Dispose();
        }
    }
}
