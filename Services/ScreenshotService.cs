using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис создания скриншотов веб-страниц через CoreWebView2.CapturePreviewAsync.
    /// Поддерживает форматы PNG и JPEG.
    /// </summary>
    public class ScreenshotService
    {
        /// <summary>
        /// Делает скриншот теку страницы WebView2 и сохраняет в файл.
        /// </summary>
        /// <param name="webView">WebView2 контрол.</param>
        /// <param name="filePath">Путь сохранения.</param>
        /// <param name="format">Формат: "png" или "jpeg".</param>
        /// <returns>Task, завершающийся успехом/ошибкой.</returns>
        public async Task<bool> CapturePageAsync(WebView2 webView, string filePath, string format = "png")
        {
            if (webView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView2 не инициализирован");

            var imageFormat = format.ToLowerInvariant() switch
            {
                "jpeg" or "jpg" => CoreWebView2CapturePreviewImageFormat.Jpeg,
                _ => CoreWebView2CapturePreviewImageFormat.Png
            };

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await webView.CoreWebView2.CapturePreviewAsync(imageFormat, stream);

            return true;
        }
    }
}
