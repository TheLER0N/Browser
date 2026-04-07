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
    /// Главное окно браузера KING.
    /// Управляет UI-элементами и связывает их с MainViewModel через MVVM.
    /// Вся бизнес-логика находится в ViewModel; code-behind используется
    /// только для обработки событий WPF-контролов, которые нельзя выразить через команды.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        // Два независимых флага: обычный maximize и полноэкранный режим.
        // Раньше был один _isMaximized для обоих режимов — это вызывало
        // race condition при быстром переключении F11 → кнопка разворачивания.
        private bool _isWindowMaximized = false;
        private bool _isFullScreen = false;

        // Сохранённое состояние окна перед входом в фуллскрин.
        // Нужно для корректного восстановления при выходе.
        private WindowState _preFullScreenWindowState;
        private double _preFullScreenWidth;
        private double _preFullScreenHeight;
        private double _preFullScreenLeft;
        private double _preFullScreenTop;
        private WindowStyle _preFullScreenWindowStyle;
        private ResizeMode _preFullScreenResizeMode;

        private Views.SettingsPage? _settingsPage;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FATAL] InitializeComponent failed: {ex}");
                System.Windows.MessageBox.Show($"XAML Error:\n{ex.Message}", "KING Browser", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(1);
                return;
            }

            try
            {
                var vm = new MainViewModel();
                DataContext = vm;

                // Инициализируем сервис stealth mode после создания оконного хэндла
                vm.StealthService.Initialize(this);

                // Анимация появления окна + Clip для скруглённых углов
                Loaded += (s, e) =>
                {
                    // При смене выбранной вкладки обновляем отображение контента
                    vm.PropertyChanged += ViewModel_PropertyChanged;

                    // Clip для скруглённых углов — вызываем после Layout
                    Dispatcher.InvokeAsync(() => ApplyClip(), System.Windows.Threading.DispatcherPriority.Loaded);
                };

                vm.StealthService.StealthModeChanged += OnStealthModeChanged;
                UpdateUrlPlaceholder();

                // Обновляем Clip при изменении размера окна
                SizeChanged += (s, e) => Dispatcher.InvokeAsync(() => ApplyClip(), System.Windows.Threading.DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FATAL] MainWindow constructor failed: {ex}");
                System.Windows.MessageBox.Show($"Initialization Error:\n{ex.Message}", "KING Browser", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(1);
            }
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

        /// <summary>
        /// Применяет Clip с CornerRadius=14 к WindowRoot — скругляет углы окна,
        /// чтобы элементы внутри (Title Bar, Tab Bar) не перекрывали скругление.
        /// </summary>
        private void ApplyClip()
        {
            var radius = 14.0;
            var rect = new System.Windows.Rect(0, 0, WindowRoot.ActualWidth, WindowRoot.ActualHeight);
            var geometry = new System.Windows.Media.RectangleGeometry(rect, radius, radius);
            WindowRoot.Clip = geometry;
        }

        /// <summary>
        /// Перетаскивание окна за title bar (когда цепляемся мышкой за верхнюю панель).
        /// </summary>
        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
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

        /// <summary>
        /// Переключает состояние обычного maximize (не фуллскрин).
        /// Заблокировано, если активен полноэкранный режим — чтобы избежать
        /// конфликта состояний (race condition F11 → кнопка).
        /// </summary>
        private void ToggleMaximize()
        {
            // Блокируем кнопку во время фуллскрина — предотвращаем конфликт
            if (_isFullScreen) return;

            _isWindowMaximized = !_isWindowMaximized;
            WindowState = _isWindowMaximized ? WindowState.Maximized : WindowState.Normal;
            BtnMaximize.Content = _isWindowMaximized ? "❐" : "□";
        }

        // ==================== Меню (Settings/History/Bookmarks) ====================

        /// <summary>
        /// Открывает/закрывает страницу настроек.
        /// При первом открытии создаёт экземпляр SettingsPage,
        /// при повторных — переиспользует его для сохранения состояния.
        /// </summary>
        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.IsSettingsOpen)
                {
                    ViewModel.CloseSettings();
                }
                else
                {
                    _settingsPage ??= new Views.SettingsPage { DataContext = ViewModel };
                    ViewModel.OpenSettings(_settingsPage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BtnMenu_Click error: {ex}");
                MessageBox.Show($"Ошибка открытия меню: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Открывает окно режима инкогнито.
        /// Инкогнито использует изолированный профиль WebView2 и не сохраняет данные.
        /// </summary>
        private void BtnIncognito_Click(object sender, RoutedEventArgs e)
        {
            var incognitoWindow = new IncognitoWindow();
            incognitoWindow.Show();
        }

        /// <summary>
        /// Показывает popup со списком профилей для переключения.
        /// </summary>
        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            ProfileList.Children.Clear();

            foreach (var profile in vm.ProfileService.Profiles)
            {
                var btn = new System.Windows.Controls.Button
                {
                    Content = $"{(profile.IsActive ? "● " : "○ ")}{profile.Name}",
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new System.Windows.Thickness(0),
                    Foreground = FindResource("TextPrimaryBrush") as System.Windows.Media.Brush,
                    Padding = new System.Windows.Thickness(10, 8, 10, 8),
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = 13,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable")
                };

                var p = profile;
                btn.Click += (s, args) =>
                {
                    vm.ProfileService.SetActiveProfile(p.Id);
                    ProfileNameText.Text = vm.ProfileService.GetActiveProfile()!.Name;
                    ProfilePopup.IsOpen = false;
                    vm.StatusText = $"Профиль: {p.Name}";
                };

                ProfileList.Children.Add(btn);
            }

            // Кнопка "Управление профилями"
            var manageBtn = new System.Windows.Controls.Button
            {
                Content = "⚙️ Управление профилями",
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new System.Windows.Thickness(0),
                Foreground = FindResource("TextSecondaryBrush") as System.Windows.Media.Brush,
                Padding = new System.Windows.Thickness(10, 8, 10, 8),
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable")
            };
            manageBtn.Click += (s, args) =>
            {
                ProfilePopup.IsOpen = false;
                _settingsPage ??= new Views.SettingsPage { DataContext = ViewModel };
                vm.OpenSettings(_settingsPage);
                Dispatcher.InvokeAsync(() => _settingsPage!.ActivateProfiles());
            };
            ProfileList.Children.Add(manageBtn);

            ProfilePopup.IsOpen = true;
        }

        private void Menu_History_Click(object sender, RoutedEventArgs e)
        {
            _settingsPage ??= new Views.SettingsPage { DataContext = ViewModel };
            ViewModel.OpenSettings(_settingsPage);
            // Активируем раздел истории
            Dispatcher.InvokeAsync(() => _settingsPage!.ActivateHistory());
        }

        private void Menu_Downloads_Click(object sender, RoutedEventArgs e)
        {
            _settingsPage ??= new Views.SettingsPage { DataContext = ViewModel };
            ViewModel.OpenSettings(_settingsPage);
            // Активируем раздел загрузок
            Dispatcher.InvokeAsync(() => _settingsPage!.ActivateDownloads());
        }

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
                "KING v1.0\n\nШахматный браузер нового поколения",
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

        /// <summary>
        /// Обработчик изменения размера окна.
        /// Вызывает перерисовку для устранения визуальных артефактов,
        /// которые возникают из-за комбинации WindowChrome + AllowsTransparency.
        /// Performed через Dispatcher.InvokeAsync с низким приоритетом, чтобы
        /// дать WPF завершить текущий layout pass.
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                InvalidateVisual();
                UpdateLayout();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // F11 — полноэкранный режим
            if (e.Key == Key.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
                return;
            }

            // Ctrl+Shift+N — новое окно инкогнито
            if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.N)
            {
                var incognitoWindow = new IncognitoWindow();
                incognitoWindow.Show();
                e.Handled = true;
                return;
            }

            ViewModel.HandleKeyboardShortcut(e.Key, e.KeyboardDevice.Modifiers);
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Переключает полноэкранный режим (F11).
        ///
        /// При входе: сохраняет текущее состояние окна и разворачивает на весь экран.
        /// При выходе: восстанавливает сохранённое состояние и вызывает перерисовку.
        ///
        /// Использует Dispatcher.InvokeAsync для безопасного обновления UI
        /// и InvalidateVisual() для корректной перерисовки после выхода.
        /// </summary>
        private void ToggleFullScreen()
        {
            try
            {
                if (!_isFullScreen)
                {
                    // === Вход в полноэкранный режим ===
                    // Сохраняем текущее состояние для последующего восстановления
                    _preFullScreenWindowState = WindowState;
                    _preFullScreenWidth = Width;
                    _preFullScreenHeight = Height;
                    _preFullScreenLeft = Left;
                    _preFullScreenTop = Top;
                    _preFullScreenWindowStyle = WindowStyle;
                    _preFullScreenResizeMode = ResizeMode;

                    _isFullScreen = true;

                    // Обновляем кнопку maximize — показываем что мы в фуллскрине
                    BtnMaximize.Content = "❐";

                    // Разворачиваем на весь экран
                    WindowStyle = WindowStyle.None;
                    ResizeMode = ResizeMode.NoResize;
                    Left = 0;
                    Top = 0;
                    Width = SystemParameters.PrimaryScreenWidth;
                    Height = SystemParameters.PrimaryScreenHeight;
                    WindowState = WindowState.Normal;

                    System.Diagnostics.Debug.WriteLine("Entered fullscreen");
                }
                else
                {
                    // === Выход из полноэкранного режима ===
                    _isFullScreen = false;

                    // Восстанавливаем сохранённое состояние
                    WindowStyle = _preFullScreenWindowStyle;
                    ResizeMode = _preFullScreenResizeMode;
                    Width = _preFullScreenWidth;
                    Height = _preFullScreenHeight;
                    Left = _preFullScreenLeft;
                    Top = _preFullScreenTop;
                    WindowState = _preFullScreenWindowState;

                    // Обновляем кнопку maximize в соответствии с восстановленным состоянием
                    _isWindowMaximized = (WindowState == WindowState.Maximized);
                    BtnMaximize.Content = _isWindowMaximized ? "❐" : "□";

                    // Вызываем перерисовку для устранения визуальных артефактов
                    Dispatcher.InvokeAsync(() =>
                    {
                        InvalidateVisual();
                        UpdateLayout();
                    }, System.Windows.Threading.DispatcherPriority.Background);

                    System.Diagnostics.Debug.WriteLine("Exited fullscreen");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleFullScreen error: {ex.Message}");

                // Аварийное восстановление: сбрасываем фуллскрин в безопасное состояние
                _isFullScreen = false;
                Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        WindowStyle = WindowStyle.None;
                        ResizeMode = ResizeMode.CanResizeWithGrip;
                        Width = 1280;
                        Height = 720;
                        WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        BtnMaximize.Content = "□";
                        InvalidateVisual();
                    }
                    catch (Exception restoreEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fullscreen restore fallback error: {restoreEx.Message}");
                    }
                });
            }
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
            // Освобождаем SettingsPage — предотвращаем утечку памяти
            _settingsPage = null;

            ViewModel.Cleanup();
            base.OnClosed(e);
        }
    }
}
