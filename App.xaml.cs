using System;
using System.IO;
using System.Windows;

namespace GhostBrowser
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_error.log");

            // Глобальный обработчик необработанных исключений UI-потока
            DispatcherUnhandledException += (s, args) =>
            {
                var msg = $"[{DateTime.Now:HH:mm:ss}] Unhandled UI exception:\n{args.Exception}\n";
                File.WriteAllText(logPath, msg);
                System.Diagnostics.Debug.WriteLine(msg);
                MessageBox.Show(
                    $"Критическая ошибка:\n\n{args.Exception.Message}\n\nПодробности в: {logPath}",
                    "KING Browser — Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            };

            // Глобальный обработчик необработанных исключений в других потоках
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    var msg = $"[{DateTime.Now:HH:mm:ss}] Unhandled domain exception:\n{ex}\n";
                    File.WriteAllText(logPath, msg);
                    System.Diagnostics.Debug.WriteLine(msg);
                    MessageBox.Show(
                        $"Фатальная ошибка:\n\n{ex.Message}\n\nПодробности в: {logPath}",
                        "KING Browser — Фатальная ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Environment.Exit(1);
                }
            };
        }
    }
}
