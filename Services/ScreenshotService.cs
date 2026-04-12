using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис скриншотов страниц.
    /// Позволяет делать скриншот видимой области и всей страницы.
    /// </summary>
    public class ScreenshotService
    {
        /// <summary>
        /// Делает скриншот видимой области WebView2 и сохраняет в файл.
        /// </summary>
        public async Task<string?> CaptureVisibleAsync(WebView2 webView, string? folder = null, string? fileName = null)
        {
            if (webView?.CoreWebView2 == null) return null;

            try
            {
                var outputFile = GetOutputPath(folder, fileName, "png");

                // Используем CapturePreview для скриншота видимой области
                using var stream = new FileStream(outputFile, FileMode.Create);
                await webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);

                System.Diagnostics.Debug.WriteLine($"[ScreenshotService] Visible capture saved: {outputFile}");
                return outputFile;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScreenshotService] CaptureVisible error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Формирует путь к файлу скриншота.
        /// </summary>
        private string GetOutputPath(string? folder, string? fileName, string extension)
        {
            if (string.IsNullOrEmpty(folder))
            {
                folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    "KingBrowser Screenshots");
            }

            Directory.CreateDirectory(folder);

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{extension}";
            }
            else if (!fileName.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
            {
                fileName = Path.ChangeExtension(fileName, extension);
            }

            return Path.Combine(folder, fileName);
        }

        /// <summary>
        /// Открывает диалог сохранения файла и возвращает путь.
        /// </summary>
        public string? ShowSaveDialog(string defaultFileName = "")
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                    FileName = string.IsNullOrEmpty(defaultFileName)
                        ? $"screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png"
                        : defaultFileName,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                return dialog.ShowDialog() == true ? dialog.FileName : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScreenshotService] ShowSaveDialog error: {ex.Message}");
                return null;
            }
        }
    }
}
