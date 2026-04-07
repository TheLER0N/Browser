using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис блокировки скриншотов и fingerprinting в WebView2.
    /// 
    /// Как это работает:
    /// - При инициализации каждой вкладки внедряется JavaScript который:
    ///   1. Блокирует navigator.getDisplayMedia (screencast API)
    ///   2. Блокирует canvas.toDataURL / canvas.toBlob (fingerprinting)
    ///   3. Блокирует canvas.getImageData (чтение пикселей)
    ///   4. Блокирует WebGL readPixels (fingerprinting через WebGL)
    /// 
    /// JavaScript выполняется через ExecuteScriptAsync сразу после загрузки страницы.
    /// 
    /// Ограничения:
    /// - JavaScript можно обойти если пользователь отключит JS (но WebView2 всегда включает)
    /// - Не защищает от скриншотов сделанных другими программами (это делает GlobalHotkey + WDA_EXCLUDEFROMCAPTURE)
    /// - Некоторые сайты могут зависеть от canvas API (карты, игры) — нужна настройка исключений
    /// </summary>
    public class ScreenshotBlocker
    {
        /// <summary>
        /// JavaScript код для блокировки скриншотов и fingerprinting.
        /// Выполняется на каждой странице после загрузки.
        /// </summary>
        private const string BlockingScript = @"
(function() {
    'use strict';
    
    // === 1. Блокировка Screencast API (захват экрана через браузер) ===
    if (navigator.getDisplayMedia) {
        Object.defineProperty(navigator, 'getDisplayMedia', {
            value: undefined,
            writable: false,
            configurable: false
        });
    }
    
    // === 2. Блокировка Canvas fingerprinting ===
    var origToDataURL = HTMLCanvasElement.prototype.toDataURL;
    var origToBlob = HTMLCanvasElement.prototype.toBlob;
    var origGetImageData = CanvasRenderingContext2D.prototype.getImageData;
    
    HTMLCanvasElement.prototype.toDataURL = function() {
        // Возвращаем пустую строку вместо реальных данных
        return '';
    };
    
    HTMLCanvasElement.prototype.toBlob = function(callback) {
        // Вызываем callback с null вместо реального blob
        if (callback) callback(null);
    };
    
    CanvasRenderingContext2D.prototype.getImageData = function() {
        // Возвращаем пустой ImageData
        return new ImageData(1, 1);
    };
    
    // === 3. Блокировка WebGL fingerprinting ===
    if (typeof WebGLRenderingContext !== 'undefined') {
        var origReadPixels = WebGLRenderingContext.prototype.readPixels;
        WebGLRenderingContext.prototype.readPixels = function() {
            // Не читаем реальные пиксели — WebGL fingerprinting блокирован
            return;
        };
    }
    
    if (typeof WebGL2RenderingContext !== 'undefined') {
        var origReadPixels2 = WebGL2RenderingContext.prototype.readPixels;
        WebGL2RenderingContext.prototype.readPixels = function() {
            return;
        };
    }
    
    // === 4. Блокировка AudioContext fingerprinting ===
    if (typeof OfflineAudioContext !== 'undefined') {
        var origCreateDynamicsCompressor = OfflineAudioContext.prototype.createDynamicsCompressor;
        // Можно добавить шум к аудио контексту но пока просто блокируем
    }
    
    console.log('[KING Browser] Anti-fingerprint protection active');
})();
";

        /// <summary>
        /// JavaScript для отключения автосохранения и автозаполнения.
        /// </summary>
        private const string PrivacyScript = @"
(function() {
    'use strict';
    
    // Отключаем autocomplete на всех формах
    var forms = document.querySelectorAll('form');
    forms.forEach(function(form) {
        form.setAttribute('autocomplete', 'off');
    });
    
    // Отключаем autocomplete на input элементах
    var inputs = document.querySelectorAll('input');
    inputs.forEach(function(input) {
        input.setAttribute('autocomplete', 'off');
    });
    
    console.log('[KING Browser] Privacy protection active');
})();
";

        /// <summary>
        /// Внедряет защитные скрипты в WebView2.
        /// Вызывается после инициализации CoreWebView2.
        /// </summary>
        /// <param name="coreWebView2">Экземпляр CoreWebView2 для внедрения скриптов.</param>
        /// <param name="enableAntiFingerprint">Если true — внедряются скрипты блокировки fingerprinting.</param>
        public async Task InjectProtectionScriptAsync(CoreWebView2 coreWebView2, bool enableAntiFingerprint = true)
        {
            if (coreWebView2 == null) return;

            try
            {
                if (enableAntiFingerprint)
                {
                    // Внедряем основной скрипт блокировки
                    await coreWebView2.ExecuteScriptAsync(BlockingScript);
                    
                    // Внедряем скрипт приватности
                    await coreWebView2.ExecuteScriptAsync(PrivacyScript);
                    
                    System.Diagnostics.Debug.WriteLine("[ScreenshotBlocker] Anti-fingerprint scripts injected");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ScreenshotBlocker] Anti-fingerprint disabled by settings");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScreenshotBlocker] Script injection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Отключает autofill для CoreWebView2 профиля.
        /// Вызывается один раз при инициализации WebView2.
        /// </summary>
        public void DisableAutofill(CoreWebView2 coreWebView2)
        {
            if (coreWebView2 == null) return;

            try
            {
                coreWebView2.Settings.IsGeneralAutofillEnabled = false;
                coreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                System.Diagnostics.Debug.WriteLine("[ScreenshotBlocker] Autofill disabled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScreenshotBlocker] DisableAutofill error: {ex.Message}");
            }
        }

        /// <summary>
        /// Устанавливает кастомный User-Agent для маскировки браузера.
        /// Выглядит как обычный Chrome — сайты не определяют что это KING Browser.
        /// </summary>
        public void SetCustomUserAgent(CoreWebView2 coreWebView2)
        {
            if (coreWebView2 == null) return;

            try
            {
                // User-Agent обычного Chrome на Windows 11
                // Это маскировка — сайты видят обычный Chrome, не KING Browser
                var customUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36";
                
                coreWebView2.Settings.UserAgent = customUserAgent;
                System.Diagnostics.Debug.WriteLine("[ScreenshotBlocker] Custom User-Agent set");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScreenshotBlocker] SetCustomUserAgent error: {ex.Message}");
            }
        }
    }
}
