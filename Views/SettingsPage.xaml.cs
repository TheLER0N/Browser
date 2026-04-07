using System;
using System.Collections.Generic;
using System.IO;
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
            // Используем актуальный ресурс после редизайна
            var active = FindResource("BgSurfaceBrush") as System.Windows.Media.Brush;
            var inactive = System.Windows.Media.Brushes.Transparent;
            if (NavDnsBtn != null) NavDnsBtn.Background = _currentSection == "DNS" ? active : inactive;
            if (NavGeneralBtn != null) NavGeneralBtn.Background = _currentSection == "Общие" ? active : inactive;
            if (NavPrivacyBtn != null) NavPrivacyBtn.Background = _currentSection == "Приватность" ? active : inactive;
            if (NavStealthBtn != null) NavStealthBtn.Background = _currentSection == "Stealth 2.0" ? active : inactive;
            if (NavHistoryBtn != null) NavHistoryBtn.Background = _currentSection == "История" ? active : inactive;
            if (NavBookmarksBtn != null) NavBookmarksBtn.Background = _currentSection == "Закладки" ? active : inactive;
            if (NavDownloadsBtn != null) NavDownloadsBtn.Background = _currentSection == "Загрузки" ? active : inactive;
            if (NavAboutBtn != null) NavAboutBtn.Background = _currentSection == "О программе" ? active : inactive;
        }

        private void NavDns_Click(object sender, RoutedEventArgs e) => ShowSection("DNS");
        private void NavGeneral_Click(object sender, RoutedEventArgs e) => ShowSection("Общие");
        private void NavPrivacy_Click(object sender, RoutedEventArgs e) => ShowSection("Приватность");
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
        /// Навигация к секции Stealth 2.0.
        /// </summary>
        private void NavStealth_Click(object sender, RoutedEventArgs e) => ShowSection("Stealth 2.0");

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
    }
}
