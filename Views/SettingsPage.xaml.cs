using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GhostBrowser.ViewModels;
using GhostBrowser.Services;

namespace GhostBrowser.Views
{
    public partial class SettingsPage : UserControl
    {
        private string _currentSection = "DNS";
        private MainViewModel? VM => DataContext as MainViewModel;
        private SettingsService? SS => VM?.SettingsService;

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

            // Настройки DNS
            UseCustomDnsToggle.IsChecked = SS.UseCustomDns;
            UseCustomDnsToggle.Checked += (s, ev) => SS.UseCustomDns = true;
            UseCustomDnsToggle.Unchecked += (s, ev) => SS.UseCustomDns = false;
            DnsInput.Text = SS.CustomDns;
            DnsInput.TextChanged += (s, ev) => SS.CustomDns = DnsInput.Text;

            // General — привязки настроены в XAML через Binding
            FontSizeSlider.Value = SS.FontSize;
            FontSizeText.Text = $"{SS.FontSize}px";
            FontSizeSlider.ValueChanged += (s, ev) => { SS.FontSize = FontSizeSlider.Value; FontSizeText.Text = $"{SS.FontSize}px"; };

            // Privacy — привязки настроены в XAML через Binding
        }

        private void ShowSection(string section)
        {
            _currentSection = section;
            if (DnsSection != null) DnsSection.Visibility = section == "DNS" ? Visibility.Visible : Visibility.Collapsed;
            if (GeneralSection != null) GeneralSection.Visibility = section == "Общие" ? Visibility.Visible : Visibility.Collapsed;
            if (PrivacySection != null) PrivacySection.Visibility = section == "Приватность" ? Visibility.Visible : Visibility.Collapsed;
            if (HistorySection != null) HistorySection.Visibility = section == "История" ? Visibility.Visible : Visibility.Collapsed;
            if (BookmarksSection != null) BookmarksSection.Visibility = section == "Закладки" ? Visibility.Visible : Visibility.Collapsed;
            if (AboutSection != null) AboutSection.Visibility = section == "О программе" ? Visibility.Visible : Visibility.Collapsed;

            if (section == "История" && VM != null) HistoryList.ItemsSource = VM.HistoryService.History;
            if (section == "Закладки" && VM != null) BookmarksList.ItemsSource = VM.BookmarkService.Bookmarks;

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
            if (NavHistoryBtn != null) NavHistoryBtn.Background = _currentSection == "История" ? active : inactive;
            if (NavBookmarksBtn != null) NavBookmarksBtn.Background = _currentSection == "Закладки" ? active : inactive;
            if (NavAboutBtn != null) NavAboutBtn.Background = _currentSection == "О программе" ? active : inactive;
        }

        private void NavDns_Click(object sender, RoutedEventArgs e) => ShowSection("DNS");
        private void NavGeneral_Click(object sender, RoutedEventArgs e) => ShowSection("Общие");
        private void NavPrivacy_Click(object sender, RoutedEventArgs e) => ShowSection("Приватность");
        private void NavHistory_Click(object sender, RoutedEventArgs e) => ShowSection("История");
        private void NavBookmarks_Click(object sender, RoutedEventArgs e) => ShowSection("Закладки");
        private void NavAbout_Click(object sender, RoutedEventArgs e) => ShowSection("О программе");

        public void ActivateHistory() { Dispatcher.InvokeAsync(() => ShowSection("История")); }
        public void ActivateBookmarks() { Dispatcher.InvokeAsync(() => ShowSection("Закладки")); }

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
    }
}
