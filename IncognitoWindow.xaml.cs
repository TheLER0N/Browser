using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GhostBrowser.ViewModels;

namespace GhostBrowser
{
    /// <summary>
    /// Окно режима инкогнито.
    /// 
    /// Особенности:
    /// - Изолированный профиль WebView2 (отдельный UserDataFolder)
    /// - Не сохраняет историю и закладки
    /// - При закрытии полностью очищает cookies, кэш и папку профиля
    /// - Фиолетовый визуальный индикатор "INCOGNITO"
    /// </summary>
    public partial class IncognitoWindow : Window
    {
        private IncognitoViewModel ViewModel => (IncognitoViewModel)DataContext;
        private bool _isMaximized = false;

        public IncognitoWindow()
        {
            InitializeComponent();
            var vm = new IncognitoViewModel();
            DataContext = vm;

            vm.StealthService.Initialize(this);

            // Анимация появления
            Loaded += (s, e) =>
            {
                if (FindResource("WindowFadeIn") is Storyboard fadeIn)
                    fadeIn.Begin(this);

                vm.PropertyChanged += ViewModel_PropertyChanged;
            };
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IncognitoViewModel.UrlInput))
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                DragMove();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                InvalidateVisual();
                UpdateLayout();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        // ==================== Закрытие вкладки (кнопка ✕ на табе) ====================

        private void CloseTabBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Находим TabViewModel из DataContext кнопки
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is TabViewModel tab)
            {
                ViewModel.CloseTab(tab);
            }
        }

        // ==================== Очистка при закрытии ====================

        protected override async void OnClosed(EventArgs e)
        {
            // Полная очистка cookies, кэша и папки профиля инкогнито
            await ViewModel.CleanupAsync();
            base.OnClosed(e);
        }

        // ==================== Горячие клавиши ====================

        protected override void OnKeyDown(KeyEventArgs e)
        {
            ViewModel.HandleKeyboardShortcut(e.Key, e.KeyboardDevice.Modifiers);
            base.OnKeyDown(e);
        }
    }
}
