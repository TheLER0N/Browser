using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using GhostBrowser.Services;

namespace GhostBrowser.ViewModels
{
    /// <summary>
    /// Главный ViewModel приложения. Управляет вкладками, навигацией и сервисами.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private TabViewModel? _selectedTab;
        private string _urlInput = "";
        private bool _isStealthMode;
        private string _statusText = "Готово";
        private string _searchEngineIcon = "G";
        private bool _isBookmarked;
        private string _clockTime = "00:00";
        private bool _isLoading;
        private bool _canGoBack;
        private bool _canGoForward;
        private object? _displayedContent;
        private bool _isSettingsOpen;

        private CoreWebView2Environment? _environment;
        private readonly System.Windows.Threading.DispatcherTimer _clockTimer;

        // Services
        public StealthService StealthService { get; }
        public HistoryService HistoryService { get; }
        public BookmarkService BookmarkService { get; }
        public SearchService SearchService { get; }
        public SettingsService SettingsService { get; }

        // ==================== Collections ====================

        public ObservableCollection<TabViewModel> Tabs { get; } = new();

        // ==================== Properties ====================

        public TabViewModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (Set(ref _selectedTab, value))
                {
                    UpdateFromSelectedTab();
                }
            }
        }

        public string UrlInput
        {
            get => _urlInput;
            set
            {
                if (Set(ref _urlInput, value))
                {
                    OnPropertyChanged(nameof(HasUrlInput));
                }
            }
        }

        public bool HasUrlInput => !string.IsNullOrEmpty(_urlInput);

        public bool IsStealthMode
        {
            get => _isStealthMode;
            set => Set(ref _isStealthMode, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        public string SearchEngineIcon
        {
            get => _searchEngineIcon;
            set => Set(ref _searchEngineIcon, value);
        }

        public bool IsBookmarked
        {
            get => _isBookmarked;
            set => Set(ref _isBookmarked, value);
        }

        public string ClockTime
        {
            get => _clockTime;
            set => Set(ref _clockTime, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
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

        /// <summary>
        /// Контент, отображаемый в основной области (WebView2 выбранной вкладки или SettingsPage).
        /// </summary>
        public object? DisplayedContent
        {
            get => _displayedContent;
            set => Set(ref _displayedContent, value);
        }

        public bool IsSettingsOpen
        {
            get => _isSettingsOpen;
            set
            {
                if (Set(ref _isSettingsOpen, value))
                {
                    OnPropertyChanged(nameof(DisplayedContent));
                }
            }
        }

        // ==================== Commands ====================

        public ICommand AddTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoForwardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand GoHomeCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand NavigateToBookmarkCommand { get; }
        public ICommand ToggleStealthCommand { get; }
        public ICommand ToggleBookmarkCommand { get; }
        public ICommand CycleSearchEngineCommand { get; }
        public ICommand FocusUrlCommand { get; }

        // ==================== Constructor ====================

        /// <summary>
        /// Инициализирует главный ViewModel приложения.
        /// 
        /// Порядок инициализации важен:
        /// 1. Создаём сервисы (не зависят друг от друга)
        /// 2. Создаём команды (ссылаются на методы ViewModel)
        /// 3. Настраиваем таймеры и подписки
        /// 4. Создаём первую вкладку (требует все сервисы)
        /// </summary>
        public MainViewModel()
        {
            StealthService = new StealthService();
            HistoryService = new HistoryService();
            BookmarkService = new BookmarkService();
            SearchService = new SearchService();
            SettingsService = new SettingsService();

            // Commands
            AddTabCommand = new RelayCommand(_ => CreateTab());
            CloseTabCommand = new RelayCommand(CloseTab, _ => Tabs.Count > 1);
            GoBackCommand = new RelayCommand(_ => SelectedTab?.GoBack(), _ => SelectedTab?.CanGoBack == true);
            GoForwardCommand = new RelayCommand(_ => SelectedTab?.GoForward(), _ => SelectedTab?.CanGoForward == true);
            RefreshCommand = new RelayCommand(_ => SelectedTab?.Reload(), _ => SelectedTab != null);
            GoHomeCommand = new RelayCommand(_ => GoHome());
            NavigateCommand = new RelayCommand(NavigateFromInput);
            NavigateToBookmarkCommand = new RelayCommand(NavigateToBookmark);
            ToggleStealthCommand = new RelayCommand(_ => ToggleStealth());
            ToggleBookmarkCommand = new RelayCommand(_ => ToggleBookmark());
            CycleSearchEngineCommand = new RelayCommand(_ => CycleSearchEngine());
            FocusUrlCommand = new RelayCommand(_ => UrlInput = SelectedTab?.Url ?? "");

            // Clock timer
            _clockTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => ClockTime = DateTime.Now.ToString("HH:mm");
            _clockTimer.Start();

            // Initialize stealth
            StealthService.StealthModeChanged += (s, stealth) => IsStealthMode = stealth;

            // Search engine
            SearchEngineIcon = SearchService.GetEngineIcon(SearchService.CurrentEngine);

            // Subscribe to bookmark changes
            BookmarkService.Bookmarks.CollectionChanged += (s, e) => UpdateBookmarkState();

            // Create first tab
            CreateTab();
        }

        // ==================== Environment ====================

        /// <summary>
        /// Подписывается на изменения свойств выбранной вкладки.
        /// При изменении CanGoBack/CanGoForward обновляет состояние команд навигации.
        /// </summary>
        private void OnSelectedTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not TabViewModel tab) return;

            if (e.PropertyName == nameof(TabViewModel.CanGoBack))
            {
                CanGoBack = tab.CanGoBack;
                if (GoBackCommand is RelayCommand cmd) cmd.RaiseCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(TabViewModel.CanGoForward))
            {
                CanGoForward = tab.CanGoForward;
                if (GoForwardCommand is RelayCommand cmd) cmd.RaiseCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(TabViewModel.IsLoading))
            {
                IsLoading = tab.IsLoading;
            }
        }

        /// <summary>
        /// Вызывается при завершении навигации в выбранной вкладке.
        /// Сохраняет посещённый URL в истории браузера.
        /// </summary>
        private void OnSelectedTabNavigationCompleted(object? sender, TabNavigationCompletedEventArgs e)
        {
            if (!e.Url.StartsWith("ghost://") && !string.IsNullOrEmpty(e.Title))
            {
                HistoryService.AddEntry(e.Title, e.Url);
            }
        }

        // ==================== Environment ====================

        private async Task<CoreWebView2Environment> GetEnvironmentAsync()
        {
            if (_environment == null)
            {
                var userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GhostBrowser");
                _environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            }
            return _environment;
        }

        // ==================== Tab Management ====================

        /// <summary>
        /// Создаёт новую вкладку с WebView2.
        /// 
        /// ВАЖНО: Метод async void, потому что вызывается из RelayCommand (не поддерживает Task).
        /// WebView2 инициализируется асинхронно внутри TabViewModel.
        /// </summary>
        /// <param name="url">Опциональный URL для навигации. Если null — открывается new tab page.</param>
        public async void CreateTab(string? url = null)
        {
            var env = await GetEnvironmentAsync();

            // WebView2 требует создание на UI потоке
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var tab = new TabViewModel(env, SearchService, url);
                Tabs.Add(tab);
                SelectedTab = tab;
                UpdateCloseTabCanExecute();
            });
        }

        /// <summary>
        /// Закрывает указанную вкладку.
        /// 
        /// Логика выбора следующей вкладки:
        /// - Если закрывается выбранная — выбираем соседнюю (следующую или предыдущую)
        /// - Если закрывается не выбранная — SelectedTab не меняется
        /// - Если это последняя вкладка — окно закрывается
        /// </summary>
        /// <param name="parameter">TabViewModel для закрытия или null для текущей выбранной.</param>
        public void CloseTab(object? parameter)
        {
            TabViewModel? tabToClose = null;

            if (parameter is TabViewModel paramTab)
            {
                tabToClose = paramTab;
            }
            else if (SelectedTab != null)
            {
                tabToClose = SelectedTab;
            }

            if (tabToClose == null) return;

            var index = Tabs.IndexOf(tabToClose);

            // Выбираем соседнюю вкладку перед закрытием
            if (SelectedTab == tabToClose)
            {
                var newIndex = Math.Min(index, Tabs.Count - 2);
                SelectedTab = Tabs[newIndex >= 0 ? newIndex : 0];
            }

            tabToClose.Dispose();
            Tabs.Remove(tabToClose);

            UpdateCloseTabCanExecute();

            // Закрываем окно если вкладок не осталось
            if (Tabs.Count == 0)
            {
                System.Windows.Application.Current.MainWindow?.Close();
            }
        }

        private void UpdateCloseTabCanExecute()
        {
            if (CloseTabCommand is RelayCommand cmd)
                cmd.RaiseCanExecuteChanged();
        }

        // ==================== Navigation ====================

        private TabViewModel? _previousSelectedTab;

        private void UpdateFromSelectedTab()
        {
            // Отписываемся от событий предыдущей выбранной вкладки
            if (_previousSelectedTab != null)
            {
                _previousSelectedTab.PropertyChanged -= OnSelectedTabPropertyChanged;
                _previousSelectedTab.NavigationCompleted -= OnSelectedTabNavigationCompleted;
            }

            if (SelectedTab != null)
            {
                // Подписываемся на события новой выбранной вкладки
                SelectedTab.PropertyChanged += OnSelectedTabPropertyChanged;
                SelectedTab.NavigationCompleted += OnSelectedTabNavigationCompleted;
                _previousSelectedTab = SelectedTab;

                UrlInput = SelectedTab.Url == "ghost://newtab" ? "" : SelectedTab.Url;
                CanGoBack = SelectedTab.CanGoBack;
                CanGoForward = SelectedTab.CanGoForward;
                IsLoading = SelectedTab.IsLoading;
                StatusText = "Готово";
                UpdateBookmarkState();

                // Если настройки закрыты, отображаем WebView выбранной вкладки
                if (!IsSettingsOpen)
                {
                    DisplayedContent = SelectedTab.WebView;
                }
            }
            else
            {
                _previousSelectedTab = null;
                UrlInput = "";
                CanGoBack = false;
                CanGoForward = false;
                IsLoading = false;
            }

            // Update command states
            if (GoBackCommand is RelayCommand backCmd) backCmd.RaiseCanExecuteChanged();
            if (GoForwardCommand is RelayCommand fwdCmd) fwdCmd.RaiseCanExecuteChanged();
            if (RefreshCommand is RelayCommand refCmd) refCmd.RaiseCanExecuteChanged();
        }

        public void NavigateFromInput(object? parameter)
        {
            if (string.IsNullOrEmpty(UrlInput)) return;
            SelectedTab?.Navigate(UrlInput);
        }

        private void NavigateToBookmark(object? parameter)
        {
            if (parameter is string url)
                NavigateToUrl(url);
        }

        public void NavigateToUrl(string url)
        {
            SelectedTab?.Navigate(url);
            UrlInput = url;
        }

        private void GoHome()
        {
            SelectedTab?.ShowNewTabPage();
            UrlInput = "";
            StatusText = "Готово";
        }

        // ==================== Bookmarks ====================

        private void ToggleBookmark()
        {
            if (SelectedTab == null) return;
            var url = SelectedTab.Url;
            if (url.StartsWith("ghost://")) return;

            if (BookmarkService.IsBookmarked(url))
            {
                var bm = BookmarkService.Bookmarks.FirstOrDefault(b => b.Url == url);
                if (bm != null)
                {
                    BookmarkService.RemoveBookmark(bm.Id);
                    StatusText = "Закладка удалена";
                }
            }
            else
            {
                BookmarkService.AddBookmark(SelectedTab.Title, url);
                StatusText = "Закладка добавлена";
            }

            UpdateBookmarkState();
        }

        private void UpdateBookmarkState()
        {
            if (SelectedTab != null)
            {
                IsBookmarked = !SelectedTab.Url.StartsWith("ghost://") &&
                               BookmarkService.IsBookmarked(SelectedTab.Url);
            }
        }

        // ==================== Stealth ====================

        private void ToggleStealth()
        {
            StealthService.ToggleStealthMode();
            StatusText = IsStealthMode ? "Защита от захвата экрана активна" : "Готово";
        }

        // ==================== Search Engine ====================

        private void CycleSearchEngine()
        {
            SearchService.CurrentEngine = SearchService.CurrentEngine switch
            {
                SearchService.SearchEngine.Google => SearchService.SearchEngine.Bing,
                SearchService.SearchEngine.Bing => SearchService.SearchEngine.DuckDuckGo,
                SearchService.SearchEngine.DuckDuckGo => SearchService.SearchEngine.Yandex,
                _ => SearchService.SearchEngine.Google
            };
            SearchEngineIcon = SearchService.GetEngineIcon(SearchService.CurrentEngine);
            StatusText = $"Поиск: {SearchService.CurrentEngine}";
        }

        // ==================== Keyboard Shortcuts ====================

        public void HandleKeyboardShortcut(Key key, ModifierKeys modifiers)
        {
            // Ctrl+T — New tab
            if (modifiers == ModifierKeys.Control && key == Key.T)
            {
                CreateTab();
                return;
            }

            // Ctrl+W — Close tab
            if (modifiers == ModifierKeys.Control && key == Key.W)
            {
                CloseTab(SelectedTab);
                return;
            }

            // Ctrl+L — Focus URL bar
            if (modifiers == ModifierKeys.Control && key == Key.L)
            {
                if (SelectedTab != null)
                    UrlInput = SelectedTab.Url == "ghost://newtab" ? "" : SelectedTab.Url;
                // Signal to focus (handled in view)
                FocusUrlRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Ctrl+Shift+H — Stealth
            if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.H)
            {
                ToggleStealth();
                return;
            }

            // Ctrl+R — Refresh
            if (modifiers == ModifierKeys.Control && key == Key.R)
            {
                SelectedTab?.Reload();
                return;
            }

            // Ctrl+D — Bookmark
            if (modifiers == ModifierKeys.Control && key == Key.D)
            {
                ToggleBookmark();
                return;
            }

            // Alt+Left — Back
            if (modifiers == ModifierKeys.Alt && key == Key.Left)
            {
                SelectedTab?.GoBack();
                return;
            }

            // Alt+Right — Forward
            if (modifiers == ModifierKeys.Alt && key == Key.Right)
            {
                SelectedTab?.GoForward();
                return;
            }

            // Ctrl+1..9 — Switch tabs
            if (modifiers == ModifierKeys.Control && key >= Key.D1 && key <= Key.D9)
            {
                int index = (int)key - (int)Key.D1;
                if (index < Tabs.Count)
                    SelectedTab = Tabs[index];
                return;
            }
        }

        // ==================== Events ====================

        /// <summary>
        /// Событие запроса фокуса на URL строке.
        /// </summary>
        public event EventHandler? FocusUrlRequested;

        // ==================== Settings ====================

        /// <summary>
        /// Открывает страницу настроек.
        /// </summary>
        /// <param name="settingsPage">Экземпляр SettingsPage для отображения.</param>
        public void OpenSettings(object settingsPage)
        {
            IsSettingsOpen = true;
            DisplayedContent = settingsPage;
            StatusText = "Меню";
        }

        /// <summary>
        /// Закрывает страницу настроек и возвращает отображение WebView.
        /// </summary>
        public void CloseSettings()
        {
            IsSettingsOpen = false;
            DisplayedContent = SelectedTab?.WebView;
            StatusText = "Готово";
        }

        // ==================== Cleanup ====================

        public void Cleanup()
        {
            _clockTimer.Stop();
            StealthService.Dispose();

            foreach (var tab in Tabs)
            {
                tab.Dispose();
            }
        }
    }
}
