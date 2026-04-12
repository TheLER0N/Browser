using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GhostBrowser.Models;
using GhostBrowser.ViewModels;
using GhostBrowser.Services;
using Forms = System.Windows.Forms;

namespace GhostBrowser.Views
{
    public partial class SettingsPage : UserControl
    {
        private string _currentSection = "DNS";
        private MainViewModel? VM => DataContext as MainViewModel;
        private SettingsService? SS => VM?.SettingsService;
        private Services.DownloadService? DS => VM?.DownloadService;

        public SettingsPage()
        {
            InitializeComponent();
            // ShowSection вызываем после Loaded, когда все элементы гарантированно созданы
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Показываем секцию DNS при первом открытии
            ShowSection("DNS");
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            if (SS == null || VM == null) return;

            // Все основные настройки привязаны через XAML Binding.
            // Только FontSizeText требует ручного обновления (отображение значения).
            FontSizeText.Text = $"{SS.FontSize}px";

            // Инициализируем поле папки загрузок
            if (DownloadFolderInput != null)
            {
                DownloadFolderInput.Text = SS.DownloadFolder;
            }
        }

        private void ShowSection(string section)
        {
            _currentSection = section;
            if (DnsSection != null) DnsSection.Visibility = section == "DNS" ? Visibility.Visible : Visibility.Collapsed;
            if (GeneralSection != null) GeneralSection.Visibility = section == "Общие" ? Visibility.Visible : Visibility.Collapsed;
            if (PrivacySection != null) PrivacySection.Visibility = section == "Приватность" ? Visibility.Visible : Visibility.Collapsed;
            if (NetworkSection != null) NetworkSection.Visibility = section == "Сеть" ? Visibility.Visible : Visibility.Collapsed;
            if (MaskingSection != null) MaskingSection.Visibility = section == "Маскировка" ? Visibility.Visible : Visibility.Collapsed;
            if (SessionsSection != null) SessionsSection.Visibility = section == "Сессии" ? Visibility.Visible : Visibility.Collapsed;
            if (AppearanceSection != null) AppearanceSection.Visibility = section == "Внешний вид" ? Visibility.Visible : Visibility.Collapsed;
            if (StealthSection != null) StealthSection.Visibility = section == "Stealth 2.0" ? Visibility.Visible : Visibility.Collapsed;
            if (HistorySection != null) HistorySection.Visibility = section == "История" ? Visibility.Visible : Visibility.Collapsed;
            if (BookmarksSection != null) BookmarksSection.Visibility = section == "Закладки" ? Visibility.Visible : Visibility.Collapsed;
            if (DownloadsSection != null) DownloadsSection.Visibility = section == "Загрузки" ? Visibility.Visible : Visibility.Collapsed;
            if (AboutSection != null) AboutSection.Visibility = section == "О программе" ? Visibility.Visible : Visibility.Collapsed;

            // При открытии Stealth 2.0 — обновляем toggle из настроек
            if (section == "Stealth 2.0" && SS != null && VM != null)
            {
                UpdateStealthToggles();
            }

            // При открытии Сети — загружаем настройки
            if (section == "Сеть" && SS != null)
            {
                LoadNetworkSettings();
            }

            // При открытии Маскировки — загружаем настройки
            if (section == "Маскировка" && SS != null)
            {
                LoadMaskingSettings();
            }

            // При открытии Сессий — обновляем список и пустое состояние
            if (section == "Сессии" && VM != null)
            {
                SessionsList.ItemsSource = VM.SessionService.Sessions;
                UpdateSessionsEmptyState();
            }

            if (section == "История" && VM != null) HistoryList.ItemsSource = VM.HistoryService.History;
            if (section == "Закладки" && VM != null) BookmarksList.ItemsSource = VM.BookmarkService.Bookmarks;

            // Привязываем коллекции загрузок
            if (section == "Загрузки" && VM != null)
            {
                if (ActiveDownloadsList != null)
                    ActiveDownloadsList.ItemsSource = VM.DownloadService.ActiveDownloads;
                if (CompletedDownloadsList != null)
                    CompletedDownloadsList.ItemsSource = VM.DownloadService.CompletedDownloads;

                // Обновляем поле папки загрузок
                if (DownloadFolderInput != null)
                    DownloadFolderInput.Text = VM.DownloadService.DownloadFolder;
            }

            UpdateNavButtons();
        }

        private void UpdateNavButtons()
        {
            var active = FindResource("BgSurfaceBrush") as System.Windows.Media.Brush;
            var inactive = System.Windows.Media.Brushes.Transparent;
            if (NavDnsBtn != null) NavDnsBtn.Background = _currentSection == "DNS" ? active : inactive;
            if (NavGeneralBtn != null) NavGeneralBtn.Background = _currentSection == "Общие" ? active : inactive;
            if (NavPrivacyBtn != null) NavPrivacyBtn.Background = _currentSection == "Приватность" ? active : inactive;
            if (NavStealthBtn != null) NavStealthBtn.Background = _currentSection == "Stealth 2.0" ? active : inactive;
            if (NavNetworkBtn != null) NavNetworkBtn.Background = _currentSection == "Сеть" ? active : inactive;
            if (NavMaskingBtn != null) NavMaskingBtn.Background = _currentSection == "Маскировка" ? active : inactive;
            if (NavSessionsBtn != null) NavSessionsBtn.Background = _currentSection == "Сессии" ? active : inactive;
            if (NavAppearanceBtn != null) NavAppearanceBtn.Background = _currentSection == "Внешний вид" ? active : inactive;
            if (NavHistoryBtn != null) NavHistoryBtn.Background = _currentSection == "История" ? active : inactive;
            if (NavBookmarksBtn != null) NavBookmarksBtn.Background = _currentSection == "Закладки" ? active : inactive;
            if (NavDownloadsBtn != null) NavDownloadsBtn.Background = _currentSection == "Загрузки" ? active : inactive;
            if (NavAboutBtn != null) NavAboutBtn.Background = _currentSection == "О программе" ? active : inactive;
        }

        private void NavDns_Click(object sender, RoutedEventArgs e) => ShowSection("DNS");
        private void NavGeneral_Click(object sender, RoutedEventArgs e) => ShowSection("Общие");
        private void NavPrivacy_Click(object sender, RoutedEventArgs e) => ShowSection("Приватность");
        private void NavStealth_Click(object sender, RoutedEventArgs e) => ShowSection("Stealth 2.0");
        private void NavNetwork_Click(object sender, RoutedEventArgs e) => ShowSection("Сеть");
        private void NavMasking_Click(object sender, RoutedEventArgs e) => ShowSection("Маскировка");
        private void NavSessions_Click(object sender, RoutedEventArgs e) => ShowSection("Сессии");
        private void NavAppearance_Click(object sender, RoutedEventArgs e) => ShowSection("Внешний вид");
        private void NavHistory_Click(object sender, RoutedEventArgs e) => ShowSection("История");
        private void NavBookmarks_Click(object sender, RoutedEventArgs e) => ShowSection("Закладки");
        private void NavDownloads_Click(object sender, RoutedEventArgs e) => ShowSection("Загрузки");
        private void NavAbout_Click(object sender, RoutedEventArgs e) => ShowSection("О программе");

        public void ActivateHistory() { Dispatcher.InvokeAsync(() => ShowSection("История")); }
        public void ActivateBookmarks() { Dispatcher.InvokeAsync(() => ShowSection("Закладки")); }
        public void ActivateDownloads() { Dispatcher.InvokeAsync(() => ShowSection("Загрузки")); }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this)?.DataContext is MainViewModel vm)
                vm.CloseSettings();
        }

        private void ClearHistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            VM?.HistoryService.ClearHistory();
            HistoryList.ItemsSource = null;
            HistoryList.ItemsSource = VM?.HistoryService.History;
        }

        private async void TestDnsBtn_Click(object sender, RoutedEventArgs e)
        {
            var dns = DnsInput.Text;
            if (string.IsNullOrWhiteSpace(dns))
            {
                MessageBox.Show("Введите DNS-адрес (например 8.8.8.8)", "Тест DNS", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            List<string> results;
            try
            {
                results = await Task.Run(() => SS!.RunDnsTestAsync(dns));
            }
            catch (Exception ex)
            {
                results = new List<string> { $"❌ Ошибка: {ex.GetType().Name}: {ex.Message}" };
            }
            var parent = Window.GetWindow(this);
            var w = new DnsTestWindow(results);
            if (parent != null) w.Owner = parent;
            w.ShowDialog();
        }

        // ==================== Download Section Handlers ====================

        /// <summary>
        /// Обработчик кнопки Pause/Resume для элемента загрузки.
        /// Использует Tag кнопки для получения привязанного DownloadItem.
        /// </summary>
        private void PauseResumeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DownloadItem item)
            {
                if (item.IsDownloading)
                    item.Pause();
                else if (item.IsPaused)
                    item.Resume();
            }
        }

        /// <summary>
        /// Обработчик кнопки отмены загрузки.
        /// </summary>
        private void CancelDownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DownloadItem item)
            {
                item.Cancel();
            }
        }

        /// <summary>
        /// Обработчик кнопки открытия файла.
        /// Запускает файл программой по умолчанию.
        /// </summary>
        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DownloadItem item)
            {
                DS?.OpenFile(item);
            }
        }

        /// <summary>
        /// Обработчик кнопки открытия папки с файлом.
        /// Открывает проводник с выделенным файлом.
        /// </summary>
        private void OpenFileLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DownloadItem item)
            {
                DS?.OpenFileLocation(item);
            }
        }

        /// <summary>
        /// Обработчик кнопки удаления загрузки.
        /// Удаляет файл с диска и запись из истории.
        /// </summary>
        private void DeleteDownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DownloadItem item)
            {
                DS?.DeleteDownload(item);
            }
        }

        /// <summary>
        /// Обработчик кнопки очистки завершённых загрузок.
        /// Удаляет все Completed/Cancelled/Failed из списка (файлы остаются на диске).
        /// </summary>
        private void ClearCompletedBtn_Click(object sender, RoutedEventArgs e)
        {
            DS?.ClearCompleted();
        }

        /// <summary>
        /// Обработчик кнопки открытия папки загрузок.
        /// </summary>
        private void OpenDownloadFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            DS?.OpenDownloadFolder();
        }

        /// <summary>
        /// Обработчик кнопки выбора папки загрузок.
        /// Открывает диалог FolderBrowserDialog и сохраняет выбор.
        /// </summary>
        private void ChooseDownloadFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DS == null) return;

            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "Выберите папку для загрузок",
                UseDescriptionForTitle = true
            };

            // Если текущая папка существует — устанавливаем как начальную
            var currentFolder = DS.DownloadFolder;
            if (Directory.Exists(currentFolder))
            {
                dialog.SelectedPath = currentFolder;
            }

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                DS.DownloadFolder = dialog.SelectedPath;

                // Обновляем поле в UI
                if (DownloadFolderInput != null)
                {
                    DownloadFolderInput.Text = dialog.SelectedPath;
                }
            }
        }

        // ==================== Stealth 2.0 ====================

        /// <summary>
        /// Обновляет Toggle-кнопки Stealth 2.0 из текущих настроек.
        /// Вызывается при открытии секции.
        /// </summary>
        private void UpdateStealthToggles()
        {
            if (SS == null) return;

            if (AutoStealthToggle != null) AutoStealthToggle.IsChecked = SS.AutoEnableStealth;
            if (BlockPrintScreenToggle != null) BlockPrintScreenToggle.IsChecked = SS.AutoBlockPrintScreen;
            if (BlockSnippingToolToggle != null) BlockSnippingToolToggle.IsChecked = SS.BlockSnippingTool;
            if (AntiFingerprintToggle != null) AntiFingerprintToggle.IsChecked = SS.AntiFingerprint;
            if (EnablePanicKeyToggle != null) EnablePanicKeyToggle.IsChecked = SS.EnablePanicKey;

            UpdateStealthStatus();
        }

        /// <summary>
        /// Обновляет текст статуса защиты.
        /// </summary>
        private void UpdateStealthStatus()
        {
            if (SS == null || StealthStatusText == null || VM == null) return;

            var parts = new List<string>();
            if (SS.AutoEnableStealth) parts.Add("Stealth: ✅");
            if (SS.AutoBlockPrintScreen) parts.Add("PrintScreen: ✅");
            if (SS.BlockSnippingTool) parts.Add("Snipping: ✅");
            if (SS.AntiFingerprint) parts.Add("Anti-FP: ✅");
            if (SS.EnablePanicKey) parts.Add("F12 Panic: ✅");

            StealthStatusText.Text = parts.Count > 0
                ? $"Активны: {string.Join(", ", parts)}"
                : "⚠️ Все защиты отключены";
        }

        private void AutoStealthToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (SS != null)
            {
                SS.AutoEnableStealth = AutoStealthToggle.IsChecked == true;
                UpdateStealthStatus();
            }
        }

        private void BlockPrintScreenToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (SS != null && VM != null)
            {
                SS.AutoBlockPrintScreen = BlockPrintScreenToggle.IsChecked == true;
                
                // Сразу применяем к сервису
                if (SS.AutoBlockPrintScreen)
                    VM.GlobalHotkeyService.EnableBlocking();
                else
                    VM.GlobalHotkeyService.DisableBlocking();
                
                UpdateStealthStatus();
            }
        }

        private void BlockSnippingToolToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (SS != null && VM != null)
            {
                SS.BlockSnippingTool = BlockSnippingToolToggle.IsChecked == true;
                
                // Сразу применяем к сервису
                if (SS.BlockSnippingTool)
                    VM.SnippingToolBlockerService.EnableBlocking();
                else
                    VM.SnippingToolBlockerService.DisableBlocking();
                
                UpdateStealthStatus();
            }
        }

        private void AntiFingerprintToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (SS != null)
            {
                SS.AntiFingerprint = AntiFingerprintToggle.IsChecked == true;
                UpdateStealthStatus();
            }
        }

        private void EnablePanicKeyToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (VM?.GlobalHotkeyService != null && SS != null)
            {
                bool isEnabled = EnablePanicKeyToggle.IsChecked == true;
                SS.EnablePanicKey = isEnabled;

                if (isEnabled)
                    VM.GlobalHotkeyService.EnablePanicKey();
                else
                    VM.GlobalHotkeyService.DisablePanicKey();

                UpdateStealthStatus();
            }
        }

        // ==================== Network / Bypass ====================

        private List<Services.ProxyEntry> _proxyList = new();

        /// <summary>
        /// Загружает настройки сети в UI.
        /// </summary>
        private void LoadNetworkSettings()
        {
            if (SS == null) return;

            var settings = SS.Settings;

            // Устанавливаем режим обхода
            if (BypassModeCombo != null)
            {
                var mode = settings.BypassMode;
                foreach (ComboBoxItem item in BypassModeCombo.Items)
                {
                    if (item.Tag?.ToString() == mode ||
                        (mode == "proxy" && item.Tag?.ToString() == $"proxy_{settings.ProxyType}"))
                    {
                        BypassModeCombo.SelectedItem = item;
                        break;
                    }
                }

                // Подписываемся на изменения
                BypassModeCombo.SelectionChanged += BypassModeCombo_SelectionChanged;
            }

            // Заполняем поля прокси
            if (ProxyServerInput != null) ProxyServerInput.Text = settings.ProxyServer;
            if (ProxyPortInput != null) ProxyPortInput.Text = settings.ProxyServerPort > 0 ? settings.ProxyServerPort.ToString() : "1080";
            if (ProxyUsernameInput != null) ProxyUsernameInput.Text = settings.ProxyUsername;
            if (ProxyPasswordInput != null) ProxyPasswordInput.Text = settings.ProxyPassword;

            // Показываем/скрываем карточку прокси
            UpdateProxyVisibility();

            // Подписываемся на изменения полей
            SubscribeProxyInputs();

            // Загружаем список бесплатных прокси
            LoadProxyList();

            // Статус
            UpdateNetworkStatus();
        }

        /// <summary>
        /// Загружает список бесплатных прокси из API.
        /// </summary>
        private async void LoadProxyList()
        {
            // Показываем статус загрузки
            if (ProxyRecommendationText != null)
                ProxyRecommendationText.Text = "⏳ Загрузка прокси из ProxyScrape API...";

            try
            {
                // Загружаем прокси из API
                _proxyList = await Services.ProxyManager.FetchProxiesAsync();

                if (ProxyListView != null)
                {
                    ProxyListView.ItemsSource = _proxyList;
                }

                var count = Services.ProxyManager.CachedCount;
                if (count > 0)
                {
                    if (ProxyRecommendationText != null)
                        ProxyRecommendationText.Text = $"Загружено {count} прокси. Нажмите «🔄 Проверить все» для проверки.";
                }
                else
                {
                    if (ProxyRecommendationText != null)
                        ProxyRecommendationText.Text = "⚠️ Не удалось загрузить прокси. Проверьте интернет-соединение.";
                }
            }
            catch (Exception ex)
            {
                if (ProxyRecommendationText != null)
                    ProxyRecommendationText.Text = $"❌ Ошибка загрузки: {ex.Message}";
            }
        }

        /// <summary>
        /// Подписывает кнопки "Apply" для каждого прокси.
        /// Вызывается после обновления списка.
        /// </summary>
        private void SubscribeApplyButtons()
        {
            if (ProxyListView == null) return;
            // Кнопки обрабатываются через ProxyApplyBtn_Click (Tag = ProxyEntry)
        }

        /// <summary>
        /// Клик по кнопке "→" у прокси — применяет его.
        /// </summary>
        private void ProxyApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: Services.ProxyEntry proxy })
            {
                ApplyProxyEntry(proxy);
            }
        }

        private void ProxyListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProxyListView?.SelectedItem is not Services.ProxyEntry proxy) return;

            ApplyProxyEntry(proxy);
            ProxyListView.SelectedItem = null;
        }

        /// <summary>
        /// Применяет выбранный прокси.
        /// </summary>
        private async void ApplyProxyEntry(Services.ProxyEntry proxy)
        {
            if (VM == null || SS == null) return;

            var settings = SS.Settings;
            settings.BypassMode = "proxy";
            settings.ProxyType = proxy.Type;
            settings.ProxyServer = proxy.Address;
            settings.ProxyServerPort = proxy.Port;
            SS.SaveSettings();

            // Обновляем UI поля
            if (BypassModeCombo != null)
            {
                foreach (ComboBoxItem item in BypassModeCombo.Items)
                {
                    if (item.Tag?.ToString() == $"proxy_{proxy.Type}")
                    {
                        BypassModeCombo.SelectedItem = item;
                        break;
                    }
                }
            }
            if (ProxyServerInput != null) ProxyServerInput.Text = proxy.Address;
            if (ProxyPortInput != null) ProxyPortInput.Text = proxy.Port.ToString();

            UpdateProxyVisibility();
            UpdateNetworkStatus();

            NetworkStatusText.Text = $"⏳ Применение {proxy.CountryFlag} {proxy.Address}:{proxy.Port}...";

            try
            {
                await VM.ReinitializeEnvironmentAsync();
            }
            catch (Exception ex)
            {
                NetworkStatusText.Text = $"❌ Ошибка: {ex.Message}";
            }
        }

        /// <summary>
        /// Проверяет все бесплатные прокси и обновляет список.
        /// </summary>
        private async void RefreshProxyListBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshProxyListBtn == null) return;

            RefreshProxyListBtn.IsEnabled = false;
            RefreshProxyListBtn.Content = "⏳  Проверка...";

            if (ProxyRecommendationText != null)
                ProxyRecommendationText.Text = "⏳ Загружаем свежие прокси из API...";

            try
            {
                // Сбрасываем кеш — загружаем заново
                await Services.ProxyManager.FetchProxiesAsync();

                if (ProxyRecommendationText != null)
                    ProxyRecommendationText.Text = $"⏳ Проверка {Services.ProxyManager.CachedCount} прокси (~15 сек)...";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                _proxyList = await Services.ProxyManager.TestAllAsync(maxConcurrent: 10, cts.Token);

                if (ProxyListView != null)
                {
                    ProxyListView.ItemsSource = null;
                    ProxyListView.ItemsSource = _proxyList;
                }

                var working = _proxyList.Count(p => p.IsWorking);
                if (ProxyRecommendationText != null)
                    ProxyRecommendationText.Text = working > 0
                        ? $"🟢 Найдено {working} рабочих прокси из {_proxyList.Count}. Нажмите → для применения."
                        : $"🔴 Ни один прокси не работает. {_proxyList.Count} проверено.";
            }
            catch (Exception ex)
            {
                if (ProxyRecommendationText != null)
                    ProxyRecommendationText.Text = $"❌ Ошибка проверки: {ex.Message}";
            }

            RefreshProxyListBtn.IsEnabled = true;
            RefreshProxyListBtn.Content = "🔄  Проверить все";
        }

        private bool _proxyInputsSubscribed;

        private void SubscribeProxyInputs()
        {
            if (_proxyInputsSubscribed) return;
            _proxyInputsSubscribed = true;

            if (ProxyServerInput != null) ProxyServerInput.TextChanged += ProxyInput_TextChanged;
            if (ProxyPortInput != null) ProxyPortInput.TextChanged += ProxyInput_TextChanged;
            if (ProxyUsernameInput != null) ProxyUsernameInput.TextChanged += ProxyInput_TextChanged;
            if (ProxyPasswordInput != null) ProxyPasswordInput.TextChanged += ProxyInput_TextChanged;
        }

        private void ProxyInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveProxySettings();
            UpdateProxyVisibility();
        }

        private async void BypassModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SS == null || BypassModeCombo?.SelectedItem is not ComboBoxItem selectedItem || VM == null) return;

            var tag = selectedItem.Tag?.ToString() ?? "none";
            var settings = SS.Settings;

            if (tag == "none")
            {
                settings.BypassMode = "none";
            }
            else if (tag == "doh_cloudflare")
            {
                settings.BypassMode = "doh_cloudflare";
            }
            else if (tag == "doh_google")
            {
                settings.BypassMode = "doh_google";
            }
            else if (tag.StartsWith("proxy_"))
            {
                settings.BypassMode = "proxy";
                settings.ProxyType = tag.Replace("proxy_", "");
            }

            UpdateProxyVisibility();
            UpdateNetworkStatus();
            SS.SaveSettings();

            // Применяем настройки к WebView2 без перезапуска
            await VM.ReinitializeEnvironmentAsync();
        }

        private void SaveProxySettings()
        {
            if (SS == null) return;

            var settings = SS.Settings;

            if (int.TryParse(ProxyPortInput?.Text, out var port))
                settings.ProxyServerPort = port;

            settings.ProxyServer = ProxyServerInput?.Text ?? "";
            settings.ProxyUsername = ProxyUsernameInput?.Text ?? "";
            settings.ProxyPassword = ProxyPasswordInput?.Text ?? "";

            // Автосохранение
            SS.SaveSettings();
            UpdateNetworkStatus();
        }

        private void UpdateProxyVisibility()
        {
            if (SS == null) return;

            var settings = SS.Settings;
            bool showProxy = settings.BypassMode == "proxy";

            if (ProxySettingsCard != null)
                ProxySettingsCard.Visibility = showProxy ? Visibility.Visible : Visibility.Collapsed;

            if (ProxyWarningCard != null)
            {
                bool hasWarning = showProxy &&
                    (string.IsNullOrWhiteSpace(settings.ProxyServer) || settings.ProxyServerPort <= 0);
                ProxyWarningCard.Visibility = hasWarning ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateNetworkStatus()
        {
            if (SS == null || NetworkStatusText == null) return;

            var settings = SS.Settings;
            var desc = Services.ProxyService.GetBypassModeDescription(settings.BypassMode);

            if (settings.BypassMode == "proxy")
            {
                bool isValid = Services.ProxyService.IsValidConfig(
                    settings.BypassMode, settings.ProxyType, settings.ProxyServer, settings.ProxyServerPort);
                NetworkStatusText.Text = isValid
                    ? $"✅ {desc} — {settings.ProxyServer}:{settings.ProxyServerPort}"
                    : $"⚠️ {desc} — настройте сервер и порт";
            }
            else
            {
                NetworkStatusText.Text = $"✅ {desc}";
            }
        }

        /// <summary>
        /// Применяет настройки прокси — пересоздаёт среду WebView2.
        /// </summary>
        private async void ApplyProxyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VM == null || SS == null) return;

            SaveProxySettings();

            // Проверяем корректность
            var settings = SS.Settings;
            if (string.IsNullOrWhiteSpace(settings.ProxyServer) || settings.ProxyServerPort <= 0)
            {
                MessageBox.Show("Укажите адрес сервера и порт прокси", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NetworkStatusText.Text = "⏳ Применение настроек прокси...";

            try
            {
                await VM.ReinitializeEnvironmentAsync();
            }
            catch (Exception ex)
            {
                NetworkStatusText.Text = $"❌ Ошибка: {ex.Message}";
            }
        }

        /// <summary>
        /// Тест доступности ключевых сайтов.
        /// </summary>
        private async void TestSitesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (TestResultsCard == null || TestResultsText == null) return;

            TestSitesBtn.IsEnabled = false;
            TestSitesBtn.Content = "⏳  Проверка...";
            TestResultsCard.Visibility = Visibility.Visible;
            TestResultsText.Text = "Проверяем доступность сайтов...";

            var sites = new[]
            {
                ("Google", "https://www.google.com", "🔍"),
                ("YouTube", "https://www.youtube.com", "📺"),
                ("Cloudflare", "https://www.cloudflare.com", "☁️"),
                ("Telegram Web", "https://web.telegram.org", "✈️"),
                ("Gemini", "https://gemini.google.com", "💎"),
            };

            var results = new List<string>();

            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };

            foreach (var (name, url, icon) in sites)
            {
                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.GetAsync(url);
                    sw.Stop();

                    if (response.IsSuccessStatusCode)
                        results.Add($"{icon} {name} — 🟢 {sw.ElapsedMilliseconds}мс");
                    else
                        results.Add($"{icon} {name} — 🟡 HTTP {(int)response.StatusCode}");
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    results.Add($"{icon} {name} — 🔴 Заблокирован");
                }
                catch (TaskCanceledException)
                {
                    results.Add($"{icon} {name} — 🔴 Таймаут");
                }
                catch (Exception ex)
                {
                    results.Add($"{icon} {name} — 🔴 {ex.GetType().Name}");
                }
            }

            // Подсчёт
            var ok = results.Count(r => r.Contains("🟢"));
            var fail = results.Count(r => r.Contains("🔴"));
            results.Add("");
            results.Add(fail == 0
                ? $"✅ Все сайты доступны ({ok}/{sites.Length})"
                : $"⚠️ {fail} сайт(ов) заблокировано, {ok}/{sites.Length} доступно");

            TestResultsText.Text = string.Join("\n", results);
            TestSitesBtn.IsEnabled = true;
            TestSitesBtn.Content = "🌐  Тест сайтов";
        }

        /// <summary>
        /// Тест прокси — проверяем доступность test сайта через текущие настройки.
        /// </summary>
        private async void TestProxyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SS == null) return;

            var server = ProxyServerInput?.Text ?? "";
            var portText = ProxyPortInput?.Text ?? "1080";

            if (string.IsNullOrWhiteSpace(server))
            {
                MessageBox.Show("Укажите адрес прокси-сервера", "Тест прокси", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(portText, out int port) || port <= 0)
            {
                MessageBox.Show("Укажите корректный порт", "Тест прокси", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NetworkStatusText.Text = $"⏳ Проверка прокси {server}:{port}...";

            try
            {
                // Простой тест — проверяем что сервер хотя бы резолвится
                // Полноценный тест прокси требует подключения
                var isValid = Services.ProxyService.IsValidConfig("proxy", SS.Settings.ProxyType, server, port);
                NetworkStatusText.Text = isValid
                    ? $"✅ Прокси {server}:{port} — настройки корректны. Перезапустите браузер для применения."
                    : $"❌ Некорректные настройки прокси";
            }
            catch (Exception ex)
            {
                NetworkStatusText.Text = $"❌ Ошибка: {ex.Message}";
            }
        }

        // ═══════════════════════════════════════════
        // Masking / User-Agent Settings
        // ═══════════════════════════════════════════

        /// <summary>
        /// Обновляет видимость пустого состояния для сессий.
        /// </summary>
        private void UpdateSessionsEmptyState()
        {
            if (VM == null || SessionsEmptyText == null) return;

            var hasSessions = VM.SessionService.Sessions.Count > 0;
            SessionsEmptyText.Visibility = hasSessions ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Загружает настройки маскировки UI.
        /// </summary>
        private void LoadMaskingSettings()
        {
            if (SS == null) return;

            // Устанавливаем выбранный пресет в ComboBox
            var preset = SS.UserAgentPreset;
            if (UserAgentPresetCombo != null)
            {
                foreach (System.Windows.Controls.ComboBoxItem item in UserAgentPresetCombo.Items)
                {
                    if (item.Tag?.ToString() == preset)
                    {
                        UserAgentPresetCombo.SelectedItem = item;
                        break;
                    }
                }
            }

            // Загружаем кастомный User-Agent
            if (CustomUserAgentInput != null)
            {
                CustomUserAgentInput.Text = SS.CustomUserAgentValue;
            }

            // Показываем/скрываем поле кастомного UA
            UpdateCustomUserAgentVisibility(preset);

            // Отображаем текущий User-Agent
            UpdateCurrentUserAgentDisplay(preset);
        }

        /// <summary>
        /// Обновляет видимость поля кастомного User-Agent.
        /// </summary>
        private void UpdateCustomUserAgentVisibility(string preset)
        {
            if (CustomUserAgentCard != null)
            {
                CustomUserAgentCard.Visibility = preset == "Custom" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Обновляет отображение текущего User-Agent.
        /// </summary>
        private void UpdateCurrentUserAgentDisplay(string preset)
        {
            if (CurrentUserAgentDisplay != null)
            {
                var customUa = SS?.CustomUserAgentValue ?? "";
                var uaString = Services.ScreenshotBlocker.GetUserAgentString(
                    (Services.UserAgentPreset)Enum.Parse(typeof(Services.UserAgentPreset), preset),
                    customUa);
                CurrentUserAgentDisplay.Text = uaString;
            }
        }

        /// <summary>
        /// Обработчик изменения пресета User-Agent.
        /// </summary>
        private void UserAgentPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserAgentPresetCombo.SelectedItem is not System.Windows.Controls.ComboBoxItem selectedItem) return;
            var preset = selectedItem.Tag?.ToString() ?? "Chrome";

            UpdateCustomUserAgentVisibility(preset);
            UpdateCurrentUserAgentDisplay(preset);

            // Сохраняем пресет
            if (SS != null)
            {
                SS.UserAgentPreset = preset;
            }
        }

        /// <summary>
        /// Применяет User-Agent и пересоздаёт вкладки.
        /// </summary>
        private async void ApplyUserAgentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;

            // Сохраняем кастомный UA если выбран Custom
            if (UserAgentPresetCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                var preset = selectedItem.Tag?.ToString() ?? "Chrome";
                if (preset == "Custom" && CustomUserAgentInput != null)
                {
                    if (SS != null)
                    {
                        SS.CustomUserAgentValue = CustomUserAgentInput.Text;
                    }
                }
            }

            ApplyUserAgentBtn.IsEnabled = false;
            NetworkStatusText.Text = "⏳ Применение User-Agent...";

            try
            {
                await VM.ApplyUserAgentPresetAsync();
                NetworkStatusText.Text = "✅ User-Agent успешно применён";
            }
            catch (Exception ex)
            {
                NetworkStatusText.Text = $"❌ Ошибка: {ex.Message}";
            }
            finally
            {
                ApplyUserAgentBtn.IsEnabled = true;
            }
        }
    }
}
