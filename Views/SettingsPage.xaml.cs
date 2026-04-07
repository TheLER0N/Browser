using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using GhostBrowser.Models;
using GhostBrowser.ViewModels;
using GhostBrowser.Services;
using Forms = System.Windows.Forms;
using Microsoft.VisualBasic;

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

            // ═══ Основные настройки ═══
            FontSizeText.Text = $"{SS.FontSize}px";
            if (DownloadFolderInput != null)
                DownloadFolderInput.Text = SS.DownloadFolder;

            // ═══ Appearance ═══
            // Theme
            if (ThemeCombo != null)
            {
                var themeTag = SS.Theme.ToLower();
                foreach (ComboBoxItem item in ThemeCombo.Items)
                {
                    if (item.Tag?.ToString()?.ToLower() == themeTag)
                    {
                        ThemeCombo.SelectedItem = item;
                        break;
                    }
                }
            }
            // Accent color radio buttons
            UpdateAccentRadioSelection(SS.AccentColor);
            if (CustomAccentColorInput != null)
            {
                CustomAccentColorInput.Text = SS.CustomAccentColor;
                CustomAccentColorInput.Visibility = SS.AccentColor == "custom" ? Visibility.Visible : Visibility.Collapsed;
            }
            // Zoom
            if (ZoomText != null)
                ZoomText.Text = $"{SS.DefaultZoomLevel:F1}x";

            // ═══ Performance ═══
            if (DownloadThreadsText != null)
                DownloadThreadsText.Text = $"{SS.DownloadThreads}";
            if (MemoryLimitInput != null)
                MemoryLimitInput.Text = SS.MemoryLimitMB.ToString();

            // ═══ Network ═══
            UpdateBypassRadioSelection(SS.BypassMode);
            if (ProxySettingsPanel != null)
                ProxySettingsPanel.Visibility = SS.BypassMode == "proxy" ? Visibility.Visible : Visibility.Collapsed;
            if (ClassicDnsPanel != null)
                ClassicDnsPanel.Visibility = (SS.BypassMode == "doh_cloudflare" || SS.BypassMode == "doh_google") ? Visibility.Collapsed : Visibility.Visible;
            if (ProxyTypeCombo != null)
            {
                var pType = SS.ProxyType.ToLower();
                foreach (ComboBoxItem item in ProxyTypeCombo.Items)
                {
                    if (item.Tag?.ToString()?.ToLower() == pType)
                    {
                        ProxyTypeCombo.SelectedItem = item;
                        break;
                    }
                }
            }
            if (ProxyServerInput != null)
                ProxyServerInput.Text = SS.ProxyServer;
            if (ProxyServerPortInput != null)
                ProxyServerPortInput.Text = SS.ProxyServerPort.ToString();
            if (ProxyUsernameInput != null)
                ProxyUsernameInput.Text = SS.ProxyUsername;
            if (ProxyPasswordInput != null)
                ProxyPasswordInput.Password = SS.ProxyPassword;
            UpdateBypassStatus();

            // ═══ Startup ═══
            UpdateStartupRadioSelection(SS.StartupMode);

            // ═══ Experimental ═══
            if (CustomUserAgentInput != null)
                CustomUserAgentInput.Text = SS.CustomUserAgent;
        }

        /// <summary>
        /// Обновляет выделение RadioButton для акцентного цвета.
        /// </summary>
        private void UpdateAccentRadioSelection(string color)
        {
            var c = color?.ToLower() ?? "blue";
            if (AccentBlue != null) AccentBlue.IsChecked = c == "blue";
            if (AccentPurple != null) AccentPurple.IsChecked = c == "purple";
            if (AccentGreen != null) AccentGreen.IsChecked = c == "green";
            if (AccentRed != null) AccentRed.IsChecked = c == "red";
            if (AccentCustom != null) AccentCustom.IsChecked = c == "custom";
        }

        /// <summary>
        /// Обновляет выделение RadioButton для режима запуска.
        /// </summary>
        private void UpdateStartupRadioSelection(string mode)
        {
            var m = mode?.ToLower() ?? "newtab";
            if (StartupNewTab != null) StartupNewTab.IsChecked = m == "newtab";
            if (StartupHomepage != null) StartupHomepage.IsChecked = m == "homepage";
            if (StartupLastSession != null) StartupLastSession.IsChecked = m == "lastsession";
            if (StartupCustomUrls != null) StartupCustomUrls.IsChecked = m == "customurls";
        }

        /// <summary>
        /// Обновляет выделение RadioButton для режима обхода блокировок.
        /// </summary>
        private void UpdateBypassRadioSelection(string mode)
        {
            var m = mode?.ToLower() ?? "none";
            if (BypassNone != null) BypassNone.IsChecked = m == "none";
            if (BypassDoHCloudflare != null) BypassDoHCloudflare.IsChecked = m == "doh_cloudflare";
            if (BypassDoHGoogle != null) BypassDoHGoogle.IsChecked = m == "doh_google";
            if (BypassProxy != null) BypassProxy.IsChecked = m == "proxy";
        }

        private void ShowSection(string section)
        {
            _currentSection = section;
            if (DnsSection != null) DnsSection.Visibility = section == "DNS" ? Visibility.Visible : Visibility.Collapsed;
            if (GeneralSection != null) GeneralSection.Visibility = section == "Общие" ? Visibility.Visible : Visibility.Collapsed;
            if (AppearanceSection != null) AppearanceSection.Visibility = section == "Внешний вид" ? Visibility.Visible : Visibility.Collapsed;
            if (PerformanceSection != null) PerformanceSection.Visibility = section == "Производительность" ? Visibility.Visible : Visibility.Collapsed;
            if (PrivacySection != null) PrivacySection.Visibility = section == "Приватность" ? Visibility.Visible : Visibility.Collapsed;
            if (AdBlockSection != null) AdBlockSection.Visibility = section == "AdBlock" ? Visibility.Visible : Visibility.Collapsed;
            if (NetworkSection != null) NetworkSection.Visibility = section == "Сеть" ? Visibility.Visible : Visibility.Collapsed;
            if (StartupSection != null) StartupSection.Visibility = section == "При запуске" ? Visibility.Visible : Visibility.Collapsed;
            if (SearchSection != null) SearchSection.Visibility = section == "Поиск" ? Visibility.Visible : Visibility.Collapsed;
            if (NotificationsSection != null) NotificationsSection.Visibility = section == "Уведомления" ? Visibility.Visible : Visibility.Collapsed;
            if (ExperimentalSection != null) ExperimentalSection.Visibility = section == "Экспериментальные" ? Visibility.Visible : Visibility.Collapsed;
            if (AutoFillSection != null) AutoFillSection.Visibility = section == "Автозаполнение" ? Visibility.Visible : Visibility.Collapsed;
            if (ProfilesSection != null) ProfilesSection.Visibility = section == "Профили" ? Visibility.Visible : Visibility.Collapsed;
            if (ScreenshotSection != null) ScreenshotSection.Visibility = section == "Скриншоты" ? Visibility.Visible : Visibility.Collapsed;
            if (HistorySection != null) HistorySection.Visibility = section == "История" ? Visibility.Visible : Visibility.Collapsed;
            if (BookmarksSection != null) BookmarksSection.Visibility = section == "Закладки" ? Visibility.Visible : Visibility.Collapsed;
            if (SyncSection != null) SyncSection.Visibility = section == "Синхронизация" ? Visibility.Visible : Visibility.Collapsed;
            if (DownloadsSection != null) DownloadsSection.Visibility = section == "Загрузки" ? Visibility.Visible : Visibility.Collapsed;
            if (AboutSection != null) AboutSection.Visibility = section == "О программе" ? Visibility.Visible : Visibility.Collapsed;

            if (section == "История" && VM != null) HistoryList.ItemsSource = VM.HistoryService.History;
            if (section == "Закладки" && VM != null) BookmarksList.ItemsSource = VM.BookmarkService.Bookmarks;

            if (section == "Скриншоты")
                InitializeScreenshotSettings();

            if (section == "Профили")
                RefreshProfilesList();

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
            // Используем актуальный ресурс после редизайна
            var active = FindResource("BgSurfaceBrush") as System.Windows.Media.Brush;
            var inactive = System.Windows.Media.Brushes.Transparent;
            if (NavDnsBtn != null) NavDnsBtn.Background = _currentSection == "DNS" ? active : inactive;
            if (NavGeneralBtn != null) NavGeneralBtn.Background = _currentSection == "Общие" ? active : inactive;
            if (NavAppearanceBtn != null) NavAppearanceBtn.Background = _currentSection == "Внешний вид" ? active : inactive;
            if (NavPerformanceBtn != null) NavPerformanceBtn.Background = _currentSection == "Производительность" ? active : inactive;
            if (NavPrivacyBtn != null) NavPrivacyBtn.Background = _currentSection == "Приватность" ? active : inactive;
            if (NavAdBlockBtn != null) NavAdBlockBtn.Background = _currentSection == "AdBlock" ? active : inactive;
            if (NavNetworkBtn != null) NavNetworkBtn.Background = _currentSection == "Сеть" ? active : inactive;
            if (NavStartupBtn != null) NavStartupBtn.Background = _currentSection == "При запуске" ? active : inactive;
            if (NavSearchBtn != null) NavSearchBtn.Background = _currentSection == "Поиск" ? active : inactive;
            if (NavNotificationsBtn != null) NavNotificationsBtn.Background = _currentSection == "Уведомления" ? active : inactive;
            if (NavExperimentalBtn != null) NavExperimentalBtn.Background = _currentSection == "Экспериментальные" ? active : inactive;
            if (NavAutoFillBtn != null) NavAutoFillBtn.Background = _currentSection == "Автозаполнение" ? active : inactive;
            if (NavProfilesBtn != null) NavProfilesBtn.Background = _currentSection == "Профили" ? active : inactive;
            if (NavScreenshotBtn != null) NavScreenshotBtn.Background = _currentSection == "Скриншоты" ? active : inactive;
            if (NavHistoryBtn != null) NavHistoryBtn.Background = _currentSection == "История" ? active : inactive;
            if (NavBookmarksBtn != null) NavBookmarksBtn.Background = _currentSection == "Закладки" ? active : inactive;
            if (NavSyncBtn != null) NavSyncBtn.Background = _currentSection == "Синхронизация" ? active : inactive;
            if (NavDownloadsBtn != null) NavDownloadsBtn.Background = _currentSection == "Загрузки" ? active : inactive;
            if (NavAboutBtn != null) NavAboutBtn.Background = _currentSection == "О программе" ? active : inactive;
        }

        private void NavDns_Click(object sender, RoutedEventArgs e) => ShowSection("DNS");
        private void NavGeneral_Click(object sender, RoutedEventArgs e) => ShowSection("Общие");
        private void NavAppearance_Click(object sender, RoutedEventArgs e) => ShowSection("Внешний вид");
        private void NavPerformance_Click(object sender, RoutedEventArgs e) => ShowSection("Производительность");
        private void NavPrivacy_Click(object sender, RoutedEventArgs e) => ShowSection("Приватность");
        private void NavAdBlock_Click(object sender, RoutedEventArgs e) => ShowSection("AdBlock");
        private void NavNetwork_Click(object sender, RoutedEventArgs e) => ShowSection("Сеть");
        private void NavStartup_Click(object sender, RoutedEventArgs e) => ShowSection("При запуске");
        private void NavSearch_Click(object sender, RoutedEventArgs e) => ShowSection("Поиск");
        private void NavNotifications_Click(object sender, RoutedEventArgs e) => ShowSection("Уведомления");
        private void NavExperimental_Click(object sender, RoutedEventArgs e) => ShowSection("Экспериментальные");
        private void NavAutoFill_Click(object sender, RoutedEventArgs e) => ShowSection("Автозаполнение");
        private void NavProfiles_Click(object sender, RoutedEventArgs e) => ShowSection("Профили");
        private void NavScreenshot_Click(object sender, RoutedEventArgs e) => ShowSection("Скриншоты");
        private void NavHistory_Click(object sender, RoutedEventArgs e) => ShowSection("История");
        private void NavBookmarks_Click(object sender, RoutedEventArgs e) => ShowSection("Закладки");
        private void NavSync_Click(object sender, RoutedEventArgs e) => ShowSection("Синхронизация");
        private void NavDownloads_Click(object sender, RoutedEventArgs e) => ShowSection("Загрузки");
        private void NavAbout_Click(object sender, RoutedEventArgs e) => ShowSection("О программе");

        public void ActivateHistory() { Dispatcher.InvokeAsync(() => ShowSection("История")); }
        public void ActivateBookmarks() { Dispatcher.InvokeAsync(() => ShowSection("Закладки")); }
        public void ActivateDownloads() { Dispatcher.InvokeAsync(() => ShowSection("Загрузки")); }
        public void ActivateAdBlock() { Dispatcher.InvokeAsync(() => ShowSection("AdBlock")); }
        public void ActivateAutoFill() { Dispatcher.InvokeAsync(() => ShowSection("Автозаполнение")); }
        public void ActivateProfiles() { Dispatcher.InvokeAsync(() => ShowSection("Профили")); }
        public void ActivateScreenshots() { Dispatcher.InvokeAsync(() => ShowSection("Скриншоты")); }

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

        // ═══ Appearance Section Handlers ═══

        private void AccentColor_Checked(object sender, RoutedEventArgs e)
        {
            if (SS == null) return;
            if (sender is RadioButton rb && rb.Tag is string color)
            {
                SS.AccentColor = color;
                // Показываем/скрываем поле кастомного цвета
                if (CustomAccentColorInput != null)
                    CustomAccentColorInput.Visibility = color == "custom" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void CustomAccentColor_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SS == null || CustomAccentColorInput == null) return;
            var hex = CustomAccentColorInput.Text.Trim();
            if (hex.StartsWith("#") && (hex.Length == 7 || hex.Length == 9))
            {
                SS.CustomAccentColor = hex;
                SS.AccentColor = "custom";
            }
        }

        // ═══ Startup Section Handlers ═══

        private void StartupMode_Checked(object sender, RoutedEventArgs e)
        {
            if (SS == null || sender is not RadioButton rb || rb.Tag is not string mode) return;
            if (rb.IsChecked == true)
            {
                SS.StartupMode = mode;
            }
        }

        // ═══ Performance Section Handlers ═══

        private void MemoryLimitInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SS == null || MemoryLimitInput == null) return;
            if (int.TryParse(MemoryLimitInput.Text, out var limit) && limit >= 0)
            {
                SS.MemoryLimitMB = limit;
            }
        }

        // ═══ Network / Bypass Handlers ═══

        private void BypassMode_Checked(object sender, RoutedEventArgs e)
        {
            if (SS == null || sender is not RadioButton rb || rb.Tag is not string mode) return;
            if (rb.IsChecked != true) return;

            SS.BypassMode = mode;

            // Показываем/скрываем настройки прокси
            if (ProxySettingsPanel != null)
                ProxySettingsPanel.Visibility = mode == "proxy" ? Visibility.Visible : Visibility.Collapsed;

            // Показываем/скрываем обычные DNS
            if (ClassicDnsPanel != null)
                ClassicDnsPanel.Visibility = (mode == "doh_cloudflare" || mode == "doh_google") ? Visibility.Collapsed : Visibility.Visible;

            // Обновляем DoH провайдера
            if (mode == "doh_cloudflare")
                SS.DoHProvider = "cloudflare";
            else if (mode == "doh_google")
                SS.DoHProvider = "google";

            // Обновляем статус
            UpdateBypassStatus();
        }

        /// <summary>
        /// Обновляет текст статуса обхода блокировок.
        /// </summary>
        private void UpdateBypassStatus()
        {
            if (BypassStatusText == null || SS == null) return;

            var mode = SS.BypassMode.ToLower();
            BypassStatusText.Text = mode switch
            {
                "none" => "🔒 Обход блокировок отключён. Используется стандартный DNS провайдера.",
                "doh_cloudflare" => "☁️ DoH Cloudflare (1.1.1.1) активен. DNS-запросы зашифрованы через HTTPS. Обходит DNS-блокировки.",
                "doh_google" => "🔵 DoH Google (8.8.8.8) активен. DNS-запросы зашифрованы через HTTPS. Обходит DNS-блокировки.",
                "proxy" => $"🌐 Прокси активен: {SS.ProxyServer}:{SS.ProxyServerPort} ({SS.ProxyType.ToUpper()}). Полный обход блокировок.",
                _ => "⚠️ Неизвестный режим"
            };
        }

        // ═══ AdBlock Section Handlers ═══

        private void ResetAdBlockCounter_Click(object sender, RoutedEventArgs e)
        {
            if (VM?.AdBlockService != null)
            {
                VM.AdBlockService.ResetBlockedCount();
                UpdateAdBlockStatus();
            }
        }

        /// <summary>
        /// Обновляет UI секции AdBlock — счётчик и статус.
        /// </summary>
        private void UpdateAdBlockStatus()
        {
            if (VM?.AdBlockService == null) return;

            var svc = VM.AdBlockService;
            var total = svc.TotalBlocked;

            if (AdBlockStatusText != null)
                AdBlockStatusText.Text = $"Заблокировано запросов: {total}";

            if (BasicFilterBlocked != null)
            {
                var basicList = svc.FilterLists.FirstOrDefault(f => f.Name == "GhostBrowser Basic Filter");
                if (basicList != null)
                    BasicFilterBlocked.Text = basicList.BlockedCount.ToString();
            }

            if (AdBlockStatusBanner != null)
            {
                var converter = new BrushConverter();
                if (svc.IsEnabled)
                {
                    AdBlockStatusBanner.Background = (Brush)converter.ConvertFrom("#1a2e1a")!;
                    AdBlockStatusBanner.BorderBrush = (Brush)converter.ConvertFrom("#107C10")!;
                }
                else
                {
                    AdBlockStatusBanner.Background = (Brush)converter.ConvertFrom("#2e1a1a")!;
                    AdBlockStatusBanner.BorderBrush = (Brush)converter.ConvertFrom("#E81123")!;
                    if (AdBlockStatusText != null)
                        AdBlockStatusText.Text = "AdBlock отключён — реклама и трекеры не блокируются";
                }
            }
        }

        // ═══ Sync / Bookmarks Import-Export Handlers ═══

        /// <summary>
        /// Экспорт закладок: SaveFileDialog → BookmarkService.ExportBookmarks.
        /// </summary>
        private void ExportBookmarksBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VM?.BookmarkService == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "JSON files|*.json|All files|*.*",
                DefaultExt = "json",
                FileName = $"bookmarks_export_{DateTime.Now:yyyy-MM-dd_HH-mm}.json",
                Title = "Экспорт закладок"
            };

            if (dialog.ShowDialog() == true)
            {
                bool success = VM.BookmarkService.ExportBookmarks(dialog.FileName);
                if (success)
                {
                    MessageBox.Show($"Закладки экспортированы в:\n{dialog.FileName}",
                        "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при экспорте закладок.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Импорт закладок: OpenFileDialog → BookmarkService.ImportAndMergeBookmarks → SyncResult.
        /// </summary>
        private void ImportBookmarksBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VM?.BookmarkService == null) return;

            var dialog = new OpenFileDialog
            {
                Filter = "JSON files|*.json|All files|*.*",
                DefaultExt = "json",
                Title = "Импорт закладок"
            };

            if (dialog.ShowDialog() == true)
            {
                var result = VM.BookmarkService.ImportAndMergeBookmarks(dialog.FileName);

                if (result.Success)
                {
                    MessageBox.Show(
                        $"Импорт завершён!\n\n" +
                        $"📊 Всего в файле: {result.TotalImported}\n" +
                        $"✅ Добавлено: {result.Added}\n" +
                        $"⏭️ Пропущено (дубликаты): {result.Skipped}\n" +
                        $"❌ Ошибок: {result.Errors}",
                        "Импорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(result.ErrorMessage,
                        "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ═══ Profile Section Handlers ═══

        private void AddProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;
            var name = VM.NewProfileName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите имя профиля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var colors = new[] { "#0078D4", "#8B5CF6", "#107C10", "#E81123", "#FF8C00", "#00BCF2" };
            var random = new Random();
            var color = colors[random.Next(colors.Length)];

            var profile = new Models.UserProfile
            {
                Name = name,
                AvatarColor = color,
                IsActive = false
            };

            VM.ProfileService.AddProfile(profile);
            VM.NewProfileName = "";
            RefreshProfilesList();
        }

        private void DeleteProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id && VM != null)
            {
                if (VM.ProfileService.Profiles.Count <= 1)
                {
                    MessageBox.Show("Нельзя удалить последний профиль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                VM.ProfileService.RemoveProfile(id);
                RefreshProfilesList();
            }
        }

        private void ActivateProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id && VM != null)
            {
                VM.ProfileService.SetActiveProfile(id);
                RefreshProfilesList();
                MessageBox.Show("Профиль будет применён при следующем запуске или создании новой вкладки.",
                    "Профиль активирован", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RenameProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id && VM != null)
            {
                var profile = VM.ProfileService.Profiles.FirstOrDefault(p => p.Id == id);
                if (profile == null) return;

                var result = Microsoft.VisualBasic.Interaction.InputBox("Новое имя профиля:", "Переименовать", profile.Name);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    VM.ProfileService.RenameProfile(id, result.Trim());
                    RefreshProfilesList();
                }
            }
        }

        /// <summary>
        /// Обновляет список профилей в ListBox.
        /// </summary>
        private void RefreshProfilesList()
        {
            if (VM == null || ProfilesListBox == null) return;
            ProfilesListBox.ItemsSource = null;
            ProfilesListBox.ItemsSource = VM.ProfileService.Profiles;
        }

        // ═══ Screenshot Section Handlers ═══

        /// <summary>
        /// Обработчик выбора папки для скриншотов.
        /// </summary>
        private void ChooseScreenshotFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SS == null) return;

            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "Выберите папку для скриншотов",
                UseDescriptionForTitle = true
            };

            var currentFolder = SS.ScreenshotFolder;
            if (Directory.Exists(currentFolder))
                dialog.SelectedPath = currentFolder;

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                SS.ScreenshotFolder = dialog.SelectedPath;
                if (ScreenshotFolderInput != null)
                    ScreenshotFolderInput.Text = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// Инициализация ComboBox формата скриншотов.
        /// Вызывается из ShowSection("Скриншоты").
        /// </summary>
        public void InitializeScreenshotSettings()
        {
            if (SS == null || ScreenshotFormatCombo == null) return;

            var format = SS.ScreenshotFormat.ToLower();
            foreach (ComboBoxItem item in ScreenshotFormatCombo.Items)
            {
                if (item.Tag?.ToString()?.ToLower() == format)
                {
                    ScreenshotFormatCombo.SelectedItem = item;
                    break;
                }
            }
        }
    }
}
