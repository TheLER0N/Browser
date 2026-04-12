using System.Windows;
using System.Windows.Controls;
using GhostBrowser.ViewModels;

namespace GhostBrowser.Views
{
    public partial class SidebarPanel : UserControl
    {
        private MainViewModel? VM => DataContext as MainViewModel;

        public SidebarPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обновляет данные при открытии панели.
        /// </summary>
        public void RefreshData()
        {
            if (VM == null) return;

            // Bookmarks
            BookmarksList.ItemsSource = VM.BookmarkService.Bookmarks;
            BookmarksEmptyText.Visibility = VM.BookmarkService.Bookmarks.Count > 0
                ? Visibility.Collapsed : Visibility.Visible;

            // History
            HistoryList.ItemsSource = VM.HistoryService.History;
            HistoryEmptyText.Visibility = VM.HistoryService.History.Count > 0
                ? Visibility.Collapsed : Visibility.Visible;

            // Downloads
            DownloadsList.ItemsSource = VM.DownloadService.ActiveDownloads;
            DownloadsEmptyText.Visibility = VM.DownloadService.ActiveDownloads.Count > 0
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void TabBookmarks_Click(object sender, RoutedEventArgs e)
        {
            BookmarksTab.Visibility = Visibility.Visible;
            HistoryTab.Visibility = Visibility.Collapsed;
            DownloadsTab.Visibility = Visibility.Collapsed;
        }

        private void TabHistory_Click(object sender, RoutedEventArgs e)
        {
            BookmarksTab.Visibility = Visibility.Collapsed;
            HistoryTab.Visibility = Visibility.Visible;
            DownloadsTab.Visibility = Visibility.Collapsed;
        }

        private void TabDownloads_Click(object sender, RoutedEventArgs e)
        {
            BookmarksTab.Visibility = Visibility.Collapsed;
            HistoryTab.Visibility = Visibility.Collapsed;
            DownloadsTab.Visibility = Visibility.Visible;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null)
            {
                VM.IsSidebarOpen = false;
            }
        }
    }
}
