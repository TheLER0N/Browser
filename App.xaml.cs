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
                System.Diagnostics.Debug.WriteLine($"Unhandled UI exception: {args.Exception}");
                args.Handled = true; // Предотвращаем краш приложения
            };

            // Глобальный обработчик необработанных исключений в других потоках
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unhandled domain exception: {ex}");
                }
            };
        }
    }
}
