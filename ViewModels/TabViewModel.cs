using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using GhostBrowser.Services;

namespace GhostBrowser.ViewModels
{
    /// <summary>
    /// ViewModel одной вкладки браузера.
    /// Оборачивает WebView2 и предоставляет свойства/команды для навигации.
    ///
    /// Жизненный цикл WebView2:
    /// 1. Конструктор: создаётся WebView2 контрол
    /// 2. InitializeWebViewAsync: async инициализация CoreWebView2
    /// 3. Navigate/ShowNewTabPage: первая навигация
    /// 4. Dispose: отписка от событий + уничтожение WebView
    ///
    /// ВАЖНО: WebView2 — тяжёлый объект, потребляющий ~50-100MB RAM на экземпляр.
    /// При закрытии вкладки обязательно вызывается Dispose() для освобождения ресурсов.
    /// </summary>
    public class TabViewModel : ViewModelBase
    {
        /// <summary>Задержка сброса прогресса после завершения навигации (мс).</summary>
        private const int ProgressResetDelayMs = 500;
        private string _title = "Новая вкладка";
        private string _url = "ghost://newtab";
        private bool _isLoading;
        private double _progress;
        private bool _canGoBack;
        private bool _canGoForward;
        private WebView2? _webView;
        private readonly CoreWebView2Environment _environment;
        private readonly SearchService _searchService;
        private readonly Services.DownloadService? _downloadService;
        private readonly Services.ScreenshotBlocker _screenshotBlocker;
        private readonly SettingsService? _settingsService;

        /// <summary>
        /// Событие завершения навигации. Вызывается из WebView_NavigationCompleted
        /// для уведомления MainViewModel о необходимости сохранить историю.
        /// </summary>
        public event EventHandler<TabNavigationCompletedEventArgs>? NavigationCompleted;

        public TabViewModel(CoreWebView2Environment environment, SearchService searchService, Services.DownloadService? downloadService = null, SettingsService? settingsService = null, string? initialUrl = null)
        {
            _environment = environment;
            _searchService = searchService;
            _downloadService = downloadService;
            _settingsService = settingsService;
            _screenshotBlocker = new Services.ScreenshotBlocker();

            if (!string.IsNullOrEmpty(initialUrl))
            {
                _url = initialUrl;
                _title = initialUrl;
            }

            WebView = CreateWebView();
        }

        // ==================== Properties ====================

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        public string Url
        {
            get => _url;
            set => Set(ref _url, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        public double Progress
        {
            get => _progress;
            set => Set(ref _progress, value);
        }

        public bool CanGoBack
        {
            get => _canGoBack;
            set => Set(ref _canGoBack, value);
        }

        public bool CanGoForward
        {
            get => _canGoForward;
            set => Set(ref _canGoForward, value);
        }

        public WebView2? WebView
        {
            get => _webView;
            private set => Set(ref _webView, value);
        }

        // ==================== WebView Creation ====================

        private WebView2 CreateWebView()
        {
            var webView = new WebView2();

            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.SourceChanged += WebView_SourceChanged;
            webView.ContentLoading += WebView_ContentLoading;
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

            _ = InitializeWebViewAsync(webView);

            return webView;
        }

        private async Task InitializeWebViewAsync(WebView2 webView)
        {
            try
            {
                // Дожидаемся инициализации CoreWebView2 — это обязательный шаг
                // перед любой навигацией. Без этого WebView не работает.
                await webView.EnsureCoreWebView2Async(_environment);

                if (webView.CoreWebView2 != null)
                {
                    // Устанавливаем тёмную тему для всех сайтов
                    webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;

                    // Отключаем autofill для защиты приватности
                    _screenshotBlocker.DisableAutofill(webView.CoreWebView2);

                    // Устанавливаем User-Agent из настроек (пресет или кастомный)
                    var uaPreset = Enum.TryParse<Services.UserAgentPreset>(_settingsService?.UserAgentPreset ?? "Chrome", out var parsedPreset)
                        ? parsedPreset
                        : Services.UserAgentPreset.Chrome;
                    var customUa = _settingsService?.CustomUserAgentValue ?? "";
                    _screenshotBlocker.SetCustomUserAgent(webView.CoreWebView2, uaPreset, customUa);

                    // Внедряем скрипты блокировки скриншотов и fingerprinting
                    // Проверяем настройку AntiFingerprint — если выключена, скрипты не внедряются
                    bool enableAntiFp = _settingsService?.AntiFingerprint ?? true;
                    await _screenshotBlocker.InjectProtectionScriptAsync(webView.CoreWebView2, enableAntiFp);
                }

                // Навигация после успешной инициализации
                if (!string.IsNullOrEmpty(_url) && _url != "ghost://newtab")
                {
                    Navigate(_url);
                }
                else
                {
                    ShowNewTabPage();
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку и показываем пользователю заглушку
                System.Diagnostics.Debug.WriteLine($"WebView2 initialization error: {ex.Message}");

                // Пробуем показать new tab даже при ошибке
                try
                {
                    ShowNewTabPage();
                }
                catch
                {
                    // Если даже new tab не загрузился — оставляем пустую вкладку
                    Title = "Ошибка инициализации";
                    Url = "error://init";
                }
            }
        }

        // ==================== Navigation ====================

        /// <summary>
        /// Выполняет навигацию на указанный URL.
        /// URL нормализуется через SearchService: поисковые запросы конвертируются
        /// в URL поисковика, обычные URL дополняются https://.
        /// </summary>
        /// <param name="url">URL или поисковый запрос.</param>
        public void Navigate(string? url)
        {
            if (string.IsNullOrEmpty(url) || WebView?.CoreWebView2 == null) return;

            var normalizedUrl = _searchService.NormalizeUrl(url);
            try
            {
                WebView.Source = new Uri(normalizedUrl);
                Url = url;
                IsLoading = true;
                Progress = 10;
                Title = url;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Показывает встроенную страницу новой вкладки (NewTabPage.html).
        /// Вызывается при создании вкладки, нажатии кнопки "Домой" или закрытии настроек.
        ///
        /// Использует Navigate с file:// URI вместо data: URI — это позволяет
        /// загружать относительные ресурсы (изображения, шрифты) из той же директории.
        /// Data URI не поддерживает загрузку внешних ресурсов из-за политики безопасности Chromium.
        /// </summary>
        public void ShowNewTabPage()
        {
            if (WebView == null) return;

            // Получаем путь к NewTabPage.html в директории сборки
            var assemblyPath = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            var htmlPath = System.IO.Path.Combine(assemblyPath, "NewTabPage.html");

            try
            {
                if (System.IO.File.Exists(htmlPath))
                {
                    // Загружаем через file:// URI — относительные пути (KING11.png) будут работать
                    var fileUri = new Uri(System.IO.Path.GetFullPath(htmlPath));
                    WebView.Source = fileUri;
                    Url = "ghost://newtab";
                    Title = "Новая вкладка";
                    IsLoading = false;
                    Progress = 0;
                }
                else
                {
                    // Фоллбэк: минимальная страница если файл не найден
                    System.Diagnostics.Debug.WriteLine($"NewTabPage.html not found at {htmlPath}");
                    WebView.NavigateToString(
                        "<html><body style=\"background:#000;color:#fff;font-family:sans-serif;" +
                        "display:flex;align-items:center;justify-content:center;height:100vh;margin:0;\">" +
                        "<h1>KING Browser</h1></body></html>");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowNewTabPage error: {ex.Message}");
                try
                {
                    WebView.NavigateToString(
                        "<html><body style=\"background:#000;color:#fff;font-family:sans-serif;" +
                        "display:flex;align-items:center;justify-content:center;height:100vh;margin:0;\">" +
                        "<h1>KING Browser</h1></body></html>");
                }
                catch { /* игнорируем */ }
            }
        }

        public void GoBack()
        {
            if (WebView?.CanGoBack == true && WebView.CoreWebView2 != null)
                WebView.GoBack();
        }

        public void GoForward()
        {
            if (WebView?.CanGoForward == true && WebView.CoreWebView2 != null)
                WebView.GoForward();
        }

        public void Reload()
        {
            if (WebView?.CoreWebView2 != null)
            {
                if (IsLoading)
                    WebView.Stop();
                else
                    WebView.Reload();
            }
        }

        public void Stop()
        {
            WebView?.Stop();
        }

        // ==================== WebView Events ====================

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Progress = 30;
            IsLoading = true;
        }

        private void WebView_ContentLoading(object? sender, CoreWebView2ContentLoadingEventArgs e)
        {
            Progress = 50;
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Progress = 100;
            IsLoading = false;

            if (e.IsSuccess && WebView != null)
            {
                var title = WebView.CoreWebView2?.DocumentTitle ?? "";
                var url = WebView.Source?.ToString() ?? "";

                if (!string.IsNullOrEmpty(title))
                    Title = title;

                if (!url.StartsWith("ghost://") && !(url.StartsWith("file://") && url.EndsWith("NewTabPage.html")))
                    Url = url;

                // Update navigation commands state
                CanGoBack = WebView.CanGoBack;
                CanGoForward = WebView.CanGoForward;
            }

            // Сбрасываем прогресс через 500мс — используем async/await вместо ContinueWith
            _ = ResetProgressAsync();

            // Уведомляем подписчиков о завершении навигации (для сохранения истории)
            var navTitle = WebView?.CoreWebView2?.DocumentTitle ?? "";
            var navUrl = WebView?.Source?.ToString() ?? "";
            NavigationCompleted?.Invoke(this, new TabNavigationCompletedEventArgs(navTitle, navUrl));
        }

        /// <summary>
        /// Сбрасывает индикатор прогресса через 500мс после завершения навигации.
        /// Вызывается из WebView_NavigationCompleted.
        /// </summary>
        private async Task ResetProgressAsync()
        {
            try
            {
                await Task.Delay(ProgressResetDelayMs);
                // Progress обновляется в UI-потоке (Dispatcher.Invoke не нужен,
                // т.к. Progress setter вызывает OnPropertyChanged, который безопасен).
                // Но для надёжности используем Dispatcher, если вызов не из UI-потока.
                if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => Progress = 0);
                }
                else
                {
                    Progress = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResetProgressAsync error: {ex.Message}");
            }
        }

        private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (WebView != null)
            {
                var url = WebView.Source?.ToString() ?? "";
                // Если это локальный файл NewTabPage.html — подменяем на ghost://newtab
                if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
                    url.EndsWith("NewTabPage.html", StringComparison.OrdinalIgnoreCase))
                {
                    Url = "ghost://newtab";
                    Title = "Новая вкладка";
                }
                else
                {
                    Url = url;
                }
                CanGoBack = WebView.CanGoBack;
                CanGoForward = WebView.CanGoForward;
            }
        }

        // ==================== Helpers ====================

        private string GetNewTabPageHtml()
        {
            var assemblyPath = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            var htmlPath = System.IO.Path.Combine(assemblyPath, "NewTabPage.html");

            // ВАЖНО: явно указываем UTF-8 — на русской Windows default encoding
            // это Windows-1251 (кириллица), что ломает UTF-8 файл с русским текстом.
            if (System.IO.File.Exists(htmlPath))
                return System.IO.File.ReadAllText(htmlPath, System.Text.Encoding.UTF8);

            return "<html><body style=\"background:#06080d;color:#f0f6fc;font-family:Segoe UI,sans-serif;" +
                   "display:flex;align-items:center;justify-content:center;height:100vh;margin:0;\">" +
                   "<h1>KingBrowser</h1></body></html>";
        }

        // ==================== Download Interception ====================

        /// <summary>
        /// Обработчик события начала загрузки файла.
        /// Перехватывает загрузку: отменяет стандартную загрузку WebView2
        /// и передаёт управление в DownloadService.
        /// </summary>
        private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            // Отменяем стандартную загрузку WebView2
            e.Cancel = true;

            // Если DownloadService доступен — передаём ему загрузку
            if (_downloadService != null)
            {
                var url = e.DownloadOperation.Uri;
                var suggestedName = e.ResultFilePath; // Предлагаемое имя файла от WebView2
                var fileName = string.IsNullOrEmpty(suggestedName)
                    ? null
                    : System.IO.Path.GetFileName(suggestedName);

                _downloadService.StartDownload(url, fileName);

                System.Diagnostics.Debug.WriteLine($"Download intercepted: {url} -> {fileName}");
            }
        }

        /// <summary>
        /// Обработчик завершения инициализации CoreWebView2.
        /// Подписывается на событие DownloadStarting после инициализации.
        /// </summary>
        private void WebView_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess && WebView?.CoreWebView2 != null)
            {
                // Подписываемся на событие начала загрузки файлов
                WebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            }
        }

        // ==================== Cleanup ====================

        /// <summary>
        /// Освобождает ресурсы WebView2.
        /// ВАЖНО: Отписка от событий выполняется ДО вызова Dispose(),
        /// чтобы предотвратить утечку памяти и возможные крахи при
        /// срабатывании событий уже удалённого объекта.
        /// </summary>
        public void Dispose()
        {
            if (WebView != null)
            {
                // Сначала отписываемся от всех событий
                WebView.NavigationStarting -= WebView_NavigationStarting;
                WebView.NavigationCompleted -= WebView_NavigationCompleted;
                WebView.SourceChanged -= WebView_SourceChanged;
                WebView.ContentLoading -= WebView_ContentLoading;
                if (WebView.CoreWebView2 != null)
                    WebView.CoreWebView2.DownloadStarting -= CoreWebView2_DownloadStarting;

                // Затем уничтожаем WebView
                WebView.Dispose();
                _webView = null;
            }
        }
    }

    /// <summary>
    /// Аргументы события завершения навигации в вкладке.
    /// Содержат заголовок и URL страницы для сохранения в истории.
    /// </summary>
    public class TabNavigationCompletedEventArgs
    {
        public string Title { get; }
        public string Url { get; }

        public TabNavigationCompletedEventArgs(string title, string url)
        {
            Title = title;
            Url = url;
        }
    }
}
