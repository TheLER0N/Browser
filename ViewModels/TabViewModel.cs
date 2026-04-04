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
        private string _title = "Новая вкладка";
        private string _url = "ghost://newtab";
        private bool _isLoading;
        private double _progress;
        private bool _canGoBack;
        private bool _canGoForward;
        private WebView2? _webView;
        private readonly CoreWebView2Environment _environment;
        private readonly SearchService _searchService;

        /// <summary>
        /// Событие завершения навигации. Вызывается из WebView_NavigationCompleted
        /// для уведомления MainViewModel о необходимости сохранить историю.
        /// </summary>
        public event EventHandler<TabNavigationCompletedEventArgs>? NavigationCompleted;

        public TabViewModel(CoreWebView2Environment environment, SearchService searchService, string? initialUrl = null)
        {
            _environment = environment;
            _searchService = searchService;

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
        /// Использует NavigateToString вместо Source для обхода ограничений file:// протокола.
        /// </summary>
        public void ShowNewTabPage()
        {
            if (WebView == null) return;

            var html = GetNewTabPageHtml();
            WebView.NavigateToString(html);
            Url = "ghost://newtab";
            Title = "Новая вкладка";
            IsLoading = false;
            Progress = 0;
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

                if (!url.StartsWith("ghost://"))
                    Url = url;

                // Update navigation commands state
                CanGoBack = WebView.CanGoBack;
                CanGoForward = WebView.CanGoForward;
            }

            // Reset progress after delay
            _ = Task.Delay(500).ContinueWith(_ =>
                System.Windows.Application.Current.Dispatcher.Invoke(() => Progress = 0));

            // Уведомляем подписчиков о завершении навигации (для сохранения истории)
            var navTitle = WebView?.CoreWebView2?.DocumentTitle ?? "";
            var navUrl = WebView?.Source?.ToString() ?? "";
            NavigationCompleted?.Invoke(this, new TabNavigationCompletedEventArgs(navTitle, navUrl));
        }

        private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (WebView != null)
            {
                var url = WebView.Source?.ToString() ?? "";
                Url = url;
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

            if (System.IO.File.Exists(htmlPath))
                return System.IO.File.ReadAllText(htmlPath);

            return "<html><body style=\"background:#06080d;color:#f0f6fc;font-family:Segoe UI,sans-serif;" +
                   "display:flex;align-items:center;justify-content:center;height:100vh;margin:0;\">" +
                   "<h1>👻 GhostBrowser</h1></body></html>";
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
