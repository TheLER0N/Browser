using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace GhostBrowser.Views
{
    public partial class DnsTestWindow : Window
    {
        public DnsTestWindow(IEnumerable<string> results)
        {
            InitializeComponent();
            ResultsList.ItemsSource = results;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    }
}
