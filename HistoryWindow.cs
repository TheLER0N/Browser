using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using GhostBrowser.Services;

namespace GhostBrowser
{
    public partial class HistoryWindow : Window
    {
        private readonly HistoryService _historyService;
        private readonly ListView _listView = new();

        public HistoryWindow(HistoryService historyService)
        {
            _historyService = historyService;
            
            Title = "История";
            Width = 700;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(17, 24, 39));
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var gridView = new GridView();
            gridView.Columns.Add(new GridViewColumn { Header = "Заголовок", DisplayMemberBinding = new Binding("Title"), Width = 300 });
            gridView.Columns.Add(new GridViewColumn { Header = "URL", DisplayMemberBinding = new Binding("Url"), Width = 250 });
            gridView.Columns.Add(new GridViewColumn { Header = "Дата", DisplayMemberBinding = new Binding("VisitedAt"), Width = 120 });

            _listView.View = gridView;
            _listView.Background = Brushes.Transparent;
            _listView.BorderThickness = new Thickness(0, 0, 0, 1);
            _listView.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81));
            _listView.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("VisitedAt", System.ComponentModel.ListSortDirection.Descending));
            
            Grid.SetRow(_listView, 0);
            grid.Children.Add(_listView);

            var bottomBar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };

            var btnClear = new Button { Content = "Очистить", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6), Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            btnClear.Click += (s, e) => { _historyService.ClearHistory(); RefreshHistory(); };

            var btnClose = new Button { Content = "Закрыть", Padding = new Thickness(16, 6, 16, 6), Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            btnClose.Click += (s, e) => Close();

            bottomBar.Children.Add(btnClear);
            bottomBar.Children.Add(btnClose);
            Grid.SetRow(bottomBar, 1);
            grid.Children.Add(bottomBar);

            Content = grid;
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            _listView.ItemsSource = null;
            _listView.ItemsSource = _historyService.History;
        }
    }
}
