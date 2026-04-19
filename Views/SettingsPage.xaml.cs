using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ShowSection("DNS");
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            if (SS == null || VM == null) return;

            FontSizeText.Text = $"{SS.FontSize}px";

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

            if (section == "Stealth 2.0" && SS != null && VM != null)
            {
                UpdateStealthToggles();
            }

            if (section == "Сеть" && SS != null)
            {
                LoadNetworkSettings();
            }

            if (section == "Маскировка" && SS != null)
            {
                LoadMaskingSettings();
            }

            if (section == "Сессии" && VM != null)
            {
                SessionsList.ItemsSource = VM.SessionService.Sessions;
                UpdateSessionsEmptyState();
            }

            if (section == "История" && VM != null) HistoryList.ItemsSource = VM.HistoryService.History;
            if (section == "Закладки" && VM != null) BookmarksList.ItemsSource = VM.BookmarkService.Bookmarks;

            if (section == "Загрузки" && VM != null)
            {
                if (ActiveDownloadsList != null)
                    ActiveDownloadsList.ItemsSource = VM.DownloadService.ActiveDownloads;
                if (CompletedDownloadsList != null)
                    CompletedDownloadsList.ItemsSource = VM.DownloadService.CompletedDownloads;

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

        private void CancelDownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DownloadItem item)
            {
                item.Cancel();
            }
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DownloadItem item)
            {
                DS?.OpenFile(item);
            }
        }

        private void OpenFileLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DownloadItem item)
            {
                DS?.OpenFileLocation(item);
            }
        }

        private void DeleteDownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DownloadItem item)
            {
                DS?.DeleteDownload(item);
            }
        }

        private void ClearCompletedBtn_Click(object sender, RoutedEventArgs e)
        {
            DS?.ClearCompleted();
        }

        private void OpenDownloadFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            DS?.OpenDownloadFolder();
        }

        private void ChooseDownloadFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DS == null) return;

            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "Выберите папку для загрузок",
                UseDescriptionForTitle = true
            };

            var currentFolder = DS.DownloadFolder;
            if (Directory.Exists(currentFolder))
            {
                dialog.SelectedPath = currentFolder;
            }

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                DS.DownloadFolder = dialog.SelectedPath;

                if (DownloadFolderInput != null)
                {
                    DownloadFolderInput.Text = dialog.SelectedPath;
                }
            }
        }

        // ==================== Stealth 2.0 ====================

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

        private void LoadNetworkSettings()
        {
            if (SS == null) return;

            var settings = SS.Settings;
            var mode = Services.ProxyService.NormalizeMode(settings.BypassMode);

            // Initialize GoodbyeDPI UI
            if (GoodbyeDpiStatusText != null)
            {
                GoodbyeDpiStatusText.Text = mode == Services.ProxyService.ModeGoodbyeDPI ? "Статус: Запущен" : "Статус: Выключен";
            }

            // Initialize DNS DoH Combo
            if (DnsModeCombo != null)
            {
                foreach (ComboBoxItem item in DnsModeCombo.Items)
                {
                    if (item.Tag?.ToString() == mode && (mode == Services.ProxyService.ModeDoHCloudflare || mode == Services.ProxyService.ModeDoHGoogle))
                    {
                        DnsModeCombo.SelectedItem = item;
                        break;
                    }
                    else if (mode != Services.ProxyService.ModeDoHCloudflare && mode != Services.ProxyService.ModeDoHGoogle && item.Tag?.ToString() == "none")
                    {
                        DnsModeCombo.SelectedItem = item;
                        break;
                    }
                }
            }

            // Initialize VPN Key Input
            if (VpnKeyInput != null)
            {
                if (mode == Services.ProxyService.ModeVpnXray)
                {
                    VpnKeyInput.Text = settings.VpnKey;
                }
                else if (Services.ProxyService.IsProxyMode(mode))
                {
                    VpnKeyInput.Text = $"{settings.ProxyServer}:{settings.ProxyServerPort}";
                }
                else
                {
                    VpnKeyInput.Text = "";
                }
            }
        }

        private async void StartGoodbyeDpiBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SS == null || VM == null) return;
            SS.Settings.BypassMode = Services.ProxyService.ModeGoodbyeDPI;
            SS.SaveSettings();
            LoadNetworkSettings();
            
            if (GoodbyeDpiStatusText != null) GoodbyeDpiStatusText.Text = "Статус: Запуск...";
            await VM.ReinitializeEnvironmentAsync();
            if (GoodbyeDpiStatusText != null) GoodbyeDpiStatusText.Text = "Статус: Запущен";
        }

        private async void StopGoodbyeDpiBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SS == null || VM == null) return;
            if (SS.Settings.BypassMode == Services.ProxyService.ModeGoodbyeDPI)
            {
                SS.Settings.BypassMode = Services.ProxyService.ModeDirect;
                SS.SaveSettings();
            }
            LoadNetworkSettings();
            await VM.ReinitializeEnvironmentAsync();
            if (GoodbyeDpiStatusText != null) GoodbyeDpiStatusText.Text = "Статус: Выключен";
        }

        private async void DnsModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SS == null || VM == null || DnsModeCombo.SelectedItem is not ComboBoxItem selectedItem) return;
            var tag = selectedItem.Tag?.ToString() ?? "none";

            // If GoodbyeDPI is active, DoH won't override BypassMode immediately here unless we want to disable GoodbyeDPI.
            // For simplicity, we just set BypassMode if we are explicitly selecting DoH.
            if (tag == "none" && SS.Settings.BypassMode != Services.ProxyService.ModeGoodbyeDPI && !Services.ProxyService.IsProxyMode(SS.Settings.BypassMode))
            {
                SS.Settings.BypassMode = Services.ProxyService.ModeDirect;
                SS.SaveSettings();
                await VM.ReinitializeEnvironmentAsync();
            }
            else if (tag != "none")
            {
                SS.Settings.BypassMode = tag;
                SS.SaveSettings();
                LoadNetworkSettings();
                await VM.ReinitializeEnvironmentAsync();
            }
        }

        private async void ApplyVpnKeyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SS == null || VM == null || VpnKeyInput == null) return;

            var key = VpnKeyInput.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;

            if (key.StartsWith("vless://") || key.StartsWith("vmess://"))
            {
                SS.Settings.VpnKey = key;
                SS.Settings.BypassMode = Services.ProxyService.ModeVpnXray;
                
                SS.SaveSettings();
                LoadNetworkSettings();
                await VM.ReinitializeEnvironmentAsync();
            }
            else
            {
                // Simple parser for IP:Port
                var parts = key.Split(':');
                if (parts.Length >= 2 && int.TryParse(parts.Last(), out int port))
                {
                    string ip = string.Join(":", parts.Take(parts.Length - 1));
                    
                    SS.Settings.ProxyServer = ip;
                    SS.Settings.ProxyServerPort = port;
                    SS.Settings.ProxyType = "socks5"; // Default to SOCKS5 for VPN keys
                    SS.Settings.BypassMode = Services.ProxyService.ModeManualProxy;
                    
                    SS.SaveSettings();
                    LoadNetworkSettings();
                    await VM.ReinitializeEnvironmentAsync();
                }
                else
                {
                    MessageBox.Show("Неверный формат ключа. Используйте формат IP:Port или vless://...", "Ошибка настройки", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void DisconnectVpnBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SS == null || VM == null) return;
            SS.Settings.BypassMode = Services.ProxyService.ModeDirect;
            SS.SaveSettings();
            LoadNetworkSettings();
            await VM.ReinitializeEnvironmentAsync();
        }

        // ==================== Masking / User-Agent ====================

        private void UpdateSessionsEmptyState()
        {
            if (VM == null || SessionsEmptyText == null) return;

            var hasSessions = VM.SessionService.Sessions.Count > 0;
            SessionsEmptyText.Visibility = hasSessions ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadMaskingSettings()
        {
            if (SS == null) return;

            var preset = SS.UserAgentPreset;
            if (UserAgentPresetCombo != null)
            {
                foreach (ComboBoxItem item in UserAgentPresetCombo.Items)
                {
                    if (item.Tag?.ToString() == preset)
                    {
                        UserAgentPresetCombo.SelectedItem = item;
                        break;
                    }
                }
            }

            if (CustomUserAgentInput != null)
            {
                CustomUserAgentInput.Text = SS.CustomUserAgentValue;
            }

            UpdateCustomUserAgentVisibility(preset);
            UpdateCurrentUserAgentDisplay(preset);
        }

        private void UpdateCustomUserAgentVisibility(string preset)
        {
            if (CustomUserAgentCard != null)
            {
                CustomUserAgentCard.Visibility = preset == "Custom" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

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

        private void UserAgentPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserAgentPresetCombo.SelectedItem is not ComboBoxItem selectedItem) return;
            var preset = selectedItem.Tag?.ToString() ?? "Chrome";

            UpdateCustomUserAgentVisibility(preset);
            UpdateCurrentUserAgentDisplay(preset);

            if (SS != null)
            {
                SS.UserAgentPreset = preset;
            }
        }

        private async void ApplyUserAgentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;

            if (UserAgentPresetCombo.SelectedItem is ComboBoxItem selectedItem)
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

            try
            {
                await VM.ApplyUserAgentPresetAsync();
            }
            catch (Exception)
            {
                // handle error
            }
            finally
            {
                ApplyUserAgentBtn.IsEnabled = true;
            }
        }
    }
}