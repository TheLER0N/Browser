using System.Windows;
using System.Windows.Controls;

namespace GhostBrowser.Views
{
    public partial class SidebarPanel : UserControl
    {
        public SidebarPanel()
        {
            InitializeComponent();
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
            if (DataContext is GhostBrowser.ViewModels.MainViewModel vm)
            {
                vm.IsSidebarOpen = false;
            }
        }
    }
}
