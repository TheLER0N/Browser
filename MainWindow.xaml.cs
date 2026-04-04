using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GhostBrowser.Services;
using GhostBrowser.ViewModels;

namespace GhostBrowser
{
    /// <summary>
    /// Главное окно браузера GhostBrowser.
    /// Управляет UI-элементами и связывает их с MainViewModel через MVVM.
    /// Вся бизнес-логика находится в ViewModel; code-behind используется
    /// только для обработки событий WPF-контролов, которые нельзя выразить через команды.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;
        private bool _isMaximized = false;
        private Views.SettingsPage? _settingsPage;

        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;

            // Инициализируем сервис stealth mode после создания оконного хэндла
            vm.StealthService.Initialize(this);

            UpdateBookmarksBar();

            // Анимация появления окна
            Loaded += (s, e) =>
            {
                if (FindResource("WindowFadeIn") is Storyboard fadeIn)
                    fadeIn.Begin(this);

                // При смене выбранной вкладки обновляем отображение контента
                vm.PropertyChanged += ViewModel_PropertyChanged;
            };

            vm.StealthService.StealthModeChanged += OnStealthModeChanged;
            UpdateUrlPlaceholder();
        }

        /// <summary>
        /// Обновляет видимость плейсхолдера адресной строки.
        /// Плейсхолдер скрыт, когда пользователь ввёл текст.
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.UrlInput))
                UpdateUrlPlaceholder();
        }

        private void UpdateUrlPlaceholder()
        {
            UrlPlaceholder.Visibility = string.IsNullOrEmpty(UrlBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ==================== Адресная строка ====================

        private void UrlBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.NavigateFromInput(UrlBox.Text);
                // Снимаем фокус после навигации
                UrlBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
        }

        private void UrlBox_GotFocus(object sender, RoutedEventArgs e) => UrlBox.SelectAll();

        // ==================== Управление окном ====================

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void ToggleMaximize()
        {
            _isMaximized = !_isMaximized;
            WindowState = _isMaximized ? WindowState.Maximized : WindowState.Normal;
            BtnMaximize.Content = _isMaximized ? "❐" : "□";
        }

        // ==================== Меню (Settings/History/Bookmarks) ====================

        /// <summary>
        /// Открывает/закрывает страницу настроек.
        /// При первом открытии создаёт экземпляр SettingsPage,
        /// при повторных — переиспользует его для сохранения состояния.
        /// </summary>
        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsSettingsOpen)
            {
                ViewModel.CloseSettings();
            }
            else
            {
                // Создаём SettingsPage один раз и переиспользуем
                _settingsPage ??= new Views.SettingsPage { DataContext = ViewModel };
                ViewModel.OpenSettings(_settingsPage);
            }
        }

        private void Menu_History_Click(object sender, RoutedEventArgs e)
        {
            _settingsPage ??= new Views.SettingsPage { DataContext = ViewModel };
            ViewModel.OpenSettings(_settingsPage);
            // Активируем раздел истории
            Dispatcher.InvokeAsync(() => _settingsPage!.ActivateHistory());
        }

        private void Menu_Downloads_Click(object sender, RoutedEventArgs e)
            => ViewModel.StatusText = "Загрузки скоро будут доступны";

        private void Menu_Bookmarks_Click(object sender, RoutedEventArgs e)
        {
            _settingsPage ??= new Views.SettingsPage { DataContext = ViewModel };
            ViewModel.OpenSettings(_settingsPage);
            Dispatcher.InvokeAsync(() => _settingsPage!.ActivateBookmarks());
        }

        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            _settingsPage ??= new Views.SettingsPage { DataContext = ViewModel };
            ViewModel.OpenSettings(_settingsPage);
        }

        private void Menu_ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.HistoryService.ClearHistory();
            ViewModel.StatusText = "История очищена";
        }

        private void Menu_About_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show(
                "GhostBrowser v1.0\n\nПриватный браузер нового поколения",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

        // ==================== Обработка окна ====================

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                DragMove();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            ViewModel.HandleKeyboardShortcut(e.Key, e.KeyboardDevice.Modifiers);
            base.OnKeyDown(e);
        }

        private void UpdateBookmarksBar()
        {
            BookmarksBar.ItemsSource = null;
            BookmarksBar.ItemsSource = ViewModel.BookmarkService.Bookmarks;
        }

        private void OnStealthModeChanged(object? sender, bool isStealth)
        {
            Dispatcher.Invoke(() =>
            {
                var brush = isStealth
                    ? FindResource("SuccessBrush") as System.Windows.Media.Brush
                    : FindResource("TextMutedBrush") as System.Windows.Media.Brush;
                StealthIndicatorBorder.Background = brush;
                StealthStatusText.Text = isStealth ? "Stealth: ON" : "Stealth: OFF";
                ViewModel.StatusText = isStealth
                    ? "Защита от захвата экрана активна"
                    : "Готово";
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.Cleanup();
            base.OnClosed(e);
        }
    }
}
