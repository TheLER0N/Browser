using System;
using System.Windows;

namespace GhostBrowser
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Глобальный обработчик необработанных исключений UI-потока
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"Необработанное исключение:\n\n{args.Exception.Message}\n\nStack Trace:\n{args.Exception.StackTrace}",
                    "KingBrowser — Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            // Глобальный обработчик необработанных исключений в других потоках
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    MessageBox.Show(
                        $"Критическая ошибка:\n\n{ex.GetType().Name}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                        "KingBrowser — Критическая ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };
        }
    }
}
