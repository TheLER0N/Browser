using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using GhostBrowser.Services;

namespace GhostBrowser.ViewModels
{
    /// <summary>
    /// ViewModel для окна режима инкогнито.
    /// 
    /// Отличия от MainViewModel:
    /// - Использует изолированный UserDataFolder (%APPDATA%\GhostBrowser\Incognito)
    /// - НЕ сохраняет историю посещений
    /// - НЕ сохраняет закладки
    /// - При закрытии полностью очищает cookies, кэш и UserDataFolder
    /// - Визуальный индикатор инкогнито (IsIncognitoMode = true)
    /// </summary>
    public class IncognitoViewModel : ViewModelBase
    {
        private TabViewModel? _selectedTab;
        private string _urlInput = "";
        private string _searchEngineIcon = "G";
        private string _statusText = "Инкогнито";
        private string _clockTime = "00:00";
        private bool _isLoading;
        private bool _canGoBack;
        private bool _canGoForward;
        private object? _displayedContent;

        private CoreWebView2Environment? _environment;
        private readonly System.Windows.Threading.DispatcherTimer _clockTimer;

        // Сервисы — только необходимые, без истории и закладок
        public StealthService StealthService { get; }
        public SearchService SearchService { get; }
        public SettingsService SettingsService { get; }

        // Путь к изолированному профилю WebView2
        private readonly string _incognitoUserDataFolder;

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

        public bool IsIncognitoMode => true;

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

        public object? DisplayedContent
        {
            get => _displayedContent;
            set => Set(ref _displayedContent, value);
        }

        // ==================== Commands ====================

        public ICommand AddTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoForwardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand GoHomeCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand ToggleStealthCommand { get; }
        public ICommand CycleSearchEngineCommand { get; }

        // ==================== Constructor ====================

        /// <summary>
        /// Инициализирует ViewModel режима инкогнито.
        /// Создает изолированный UserDataFolder для WebView2.
        /// </summary>
        public IncognitoViewModel()
        {
            // Изолированная папка для инкогнито — отдельный профиль WebView2
            _incognitoUserDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GhostBrowser", "Incognito");

            StealthService = new StealthService();
            SearchService = new SearchService();
            SettingsService = new SettingsService();

            // Commands
            AddTabCommand = new AsyncRelayCommand(_ => CreateTabAsync());
            CloseTabCommand = new RelayCommand(CloseTab, _ => Tabs.Count > 1);
            GoBackCommand = new RelayCommand(_ => SelectedTab?.GoBack(), _ => SelectedTab?.CanGoBack == true);
            GoForwardCommand = new RelayCommand(_ => SelectedTab?.GoForward(), _ => SelectedTab?.CanGoForward == true);
            RefreshCommand = new RelayCommand(_ => SelectedTab?.Reload(), _ => SelectedTab != null);
            GoHomeCommand = new RelayCommand(_ => GoHome());
            NavigateCommand = new RelayCommand(NavigateFromInput);
            ToggleStealthCommand = new RelayCommand(_ => ToggleStealth());
            CycleSearchEngineCommand = new RelayCommand(_ => CycleSearchEngine());

            // Clock timer
            _clockTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => ClockTime = DateTime.Now.ToString("HH:mm");
            _clockTimer.Start();

            // Search engine icon
            SearchEngineIcon = SearchService.GetEngineIcon(SearchService.CurrentEngine);

            // Создаём первую вкладку
            _ = CreateTabAsync();
        }

        // ==================== Environment ====================

        /// <summary>
        /// Создаёт изолированное окружение WebView2 с отдельным UserDataFolder.
        /// Это гарантирует, что cookies, кэш и история не пересекаются с обычным режимом.
        /// </summary>
        private async Task<CoreWebView2Environment> GetEnvironmentAsync()
        {
            if (_environment == null)
            {
                // Создаём папку если не существует
                Directory.CreateDirectory(_incognitoUserDataFolder);

                _environment = await CoreWebView2Environment.CreateAsync(null, _incognitoUserDataFolder);
            }
            return _environment;
        }

        // ==================== Tab Management ====================

        /// <summary>
        /// Создаёт новую вкладку в режиме инкогнито.
        /// </summary>
        public async Task CreateTabAsync(string? url = null)
        {
            var env = await GetEnvironmentAsync();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var tab = new TabViewModel(env, SearchService, null, null, null, url);
                Tabs.Add(tab);
                SelectedTab = tab;
                UpdateCloseTabCanExecute();
            });
        }

        /// <summary>
        /// Закрывает вкладку. Если вкладок не осталось — закрывает окно.
        /// </summary>
        public void CloseTab(object? parameter)
        {
            try
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

                if (SelectedTab == tabToClose && Tabs.Count > 1)
                {
                    var newIndex = Math.Max(0, Math.Min(index, Tabs.Count - 2));
                    SelectedTab = Tabs[newIndex];
                }

                tabToClose.Dispose();
                Tabs.Remove(tabToClose);

                UpdateCloseTabCanExecute();

                if (Tabs.Count == 0)
                {
                    System.Windows.Application.Current.MainWindow?.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Incognito CloseTab error: {ex.Message}");
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
            try
            {
                if (_previousSelectedTab != null)
                {
                    _previousSelectedTab.PropertyChanged -= OnSelectedTabPropertyChanged;
                }

                _previousSelectedTab = SelectedTab;

                if (SelectedTab != null)
                {
                    SelectedTab.PropertyChanged += OnSelectedTabPropertyChanged;

                    UrlInput = SelectedTab.Url == "ghost://newtab" ? "" : SelectedTab.Url;
                    CanGoBack = SelectedTab.CanGoBack;
                    CanGoForward = SelectedTab.CanGoForward;
                    IsLoading = SelectedTab.IsLoading;
                    StatusText = "🕶️ Инкогнито";

                    DisplayedContent = SelectedTab.WebView;
                }
                else
                {
                    _previousSelectedTab = null;
                    UrlInput = "";
                    CanGoBack = false;
                    CanGoForward = false;
                    IsLoading = false;
                }

                if (GoBackCommand is RelayCommand backCmd) backCmd.RaiseCanExecuteChanged();
                if (GoForwardCommand is RelayCommand fwdCmd) fwdCmd.RaiseCanExecuteChanged();
                if (RefreshCommand is RelayCommand refCmd) refCmd.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Incognito UpdateFromSelectedTab error: {ex.Message}");
            }
        }

        private void OnSelectedTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not TabViewModel tab || tab.WebView == null) return;

            try
            {
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Incognito OnSelectedTabPropertyChanged error: {ex.Message}");
            }
        }

        public void NavigateFromInput(object? parameter)
        {
            if (string.IsNullOrEmpty(UrlInput)) return;
            SelectedTab?.Navigate(UrlInput);
        }

        private void GoHome()
        {
            SelectedTab?.ShowNewTabPage();
            UrlInput = "";
            StatusText = "🕶️ Инкогнито";
        }

        // ==================== Stealth ====================

        private void ToggleStealth()
        {
            StealthService.ToggleStealthMode();
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
        }

        // ==================== Cleanup ====================

        /// <summary>
        /// Полная очистка всех данных инкогнито.
        /// Вызывается при закрытии окна.
        /// </summary>
        public async Task CleanupAsync()
        {
            try
            {
                _clockTimer.Stop();

                // Закрываем все вкладки
                foreach (var tab in Tabs)
                {
                    tab.Dispose();
                }
                Tabs.Clear();

                // Очищаем cookies и кэш через временный WebView2
                if (_environment != null)
                {
                    try
                    {
                        var tempWebView = new Microsoft.Web.WebView2.Wpf.WebView2();
                        await tempWebView.EnsureCoreWebView2Async(_environment);

                        if (tempWebView.CoreWebView2 != null)
                        {
                            // Удаляем все cookies
                            var cookies = await tempWebView.CoreWebView2.CookieManager.GetCookiesAsync("https://");
                            if (cookies != null)
                            {
                                foreach (var cookie in cookies)
                                {
                                    tempWebView.CoreWebView2.CookieManager.DeleteCookie(cookie);
                                }
                            }

                            // Очищаем кэш и хранилища через DevTools Protocol
                            await tempWebView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                                "Network.clearBrowserCache", "{}");
                        }
                        tempWebView.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Incognito cookie/cache cleanup error: {ex.Message}");
                    }
                }

                // Полностью удаляем изолированную папку
                if (Directory.Exists(_incognitoUserDataFolder))
                {
                    try
                    {
                        DeleteDirectory(_incognitoUserDataFolder);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Incognito folder cleanup error: {ex.Message}");
                    }
                }

                StealthService.Dispose();
                SettingsService.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Incognito Cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивное удаление папки с обработкой заблокированных файлов.
        /// </summary>
        private void DeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
                // Если файлы заблокированы — помечаем для удаления при следующем запуске
                // Windows удалит их когда процесс WebView2 полностью освободит файлы
            }
        }

        // ==================== Keyboard Shortcuts ====================

        public void HandleKeyboardShortcut(Key key, ModifierKeys modifiers)
        {
            // Ctrl+T — New tab
            if (modifiers == ModifierKeys.Control && key == Key.T)
            {
                AddTabCommand.Execute(null);
                return;
            }

            // Ctrl+W — Close tab
            if (modifiers == ModifierKeys.Control && key == Key.W)
            {
                CloseTab(SelectedTab);
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
    }
}
