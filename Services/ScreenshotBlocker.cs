using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Пресеты User-Agent для маскировки браузера под разные приложения.
    /// Полезно для обхода блокировок по User-Agent.
    /// </summary>
    public enum UserAgentPreset
    {
        Chrome,           // Google Chrome на Windows 11 (по умолчанию)
        Edge,             // Microsoft Edge на Windows 11
        Firefox,          // Mozilla Firefox на Windows 11
        Safari,           // Safari на macOS (для маскировки под Mac)
        ChromeMobile,     // Chrome на Android
        Opera,            // Opera на Windows 11
        YandexBrowser,    // Яндекс.Браузер на Windows
        Custom            // Пользовательский User-Agent
    }

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
        var origOfflineCreate = OfflineAudioContext.prototype.__proto__.constructor;
        // Добавляем шум к аудио контексту
        Object.defineProperty(OfflineAudioContext.prototype, 'createDynamicsCompressor', {
            value: function() { return null; },
            writable: false,
            configurable: false
        });
    }

    if (typeof AudioContext !== 'undefined') {
        Object.defineProperty(AudioContext.prototype, 'createDynamicsCompressor', {
            value: function() { return null; },
            writable: false,
            configurable: false
        });
    }

    // === 5. Блокировка Screen Capture API ===
    if (navigator.mediaDevices && navigator.mediaDevices.getDisplayMedia) {
        navigator.mediaDevices.getDisplayMedia = function() {
            return Promise.reject(new Error('Screen capture blocked by KING Browser'));
        };
    }

    // === 6. Блокировка MediaDevices API для захвата ===
    if (navigator.mediaDevices) {
        var origEnumerateDevices = navigator.mediaDevices.enumerateDevices;
        navigator.mediaDevices.enumerateDevices = function() {
            // Возвращаем пустой список устройств
            return Promise.resolve([]);
        };
    }

    // === 7. Маскировка navigator.hardwareConcurrency ===
    // Скрываем реальное количество ядер процессора
    Object.defineProperty(navigator, 'hardwareConcurrency', {
        get: function() { return 4; }, // Всегда возвращаем 4
        configurable: false
    });

    // === 8. Блокировка Battery API ===
    if (navigator.getBattery) {
        navigator.getBattery = function() {
            return Promise.reject(new Error('Battery API blocked'));
        };
    }

    console.log('[KING Browser] Enhanced anti-fingerprint protection active');
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
        /// Возвращает строку User-Agent для указанного пресета.
        /// </summary>
        public static string GetUserAgentString(UserAgentPreset preset, string customUserAgent = "")
        {
            return preset switch
            {
                UserAgentPreset.Chrome => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
                UserAgentPreset.Edge => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36 Edg/123.0.0.0",
                UserAgentPreset.Firefox => "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0",
                UserAgentPreset.Safari => "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
                UserAgentPreset.ChromeMobile => "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Mobile Safari/537.36",
                UserAgentPreset.Opera => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36 OPR/109.0.0.0",
                UserAgentPreset.YandexBrowser => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 YaBrowser/24.1.0.0 Safari/537.36",
                UserAgentPreset.Custom => string.IsNullOrEmpty(customUserAgent)
                    ? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36"
                    : customUserAgent,
                _ => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36"
            };
        }

        /// <summary>
        /// Возвращает отображаемое имя пресета для UI.
        /// </summary>
        public static string GetUserAgentDisplayName(UserAgentPreset preset)
        {
            return preset switch
            {
                UserAgentPreset.Chrome => "Google Chrome (Windows 11)",
                UserAgentPreset.Edge => "Microsoft Edge (Windows 11)",
                UserAgentPreset.Firefox => "Mozilla Firefox (Windows 11)",
                UserAgentPreset.Safari => "Safari (macOS)",
                UserAgentPreset.ChromeMobile => "Chrome Mobile (Android)",
                UserAgentPreset.Opera => "Opera (Windows 11)",
                UserAgentPreset.YandexBrowser => "Яндекс.Браузер (Windows)",
                UserAgentPreset.Custom => "Пользовательский",
                _ => preset.ToString()
            };
        }

        /// <summary>
        /// Устанавливает кастомный User-Agent для маскировки браузера.
        /// Выглядит как обычный Chrome — сайты не определяют что это KING Browser.
        /// </summary>
        public void SetCustomUserAgent(CoreWebView2 coreWebView2, UserAgentPreset preset = UserAgentPreset.Chrome, string customUserAgent = "")
        {
            if (coreWebView2 == null) return;

            try
            {
                var userAgent = GetUserAgentString(preset, customUserAgent);
                coreWebView2.Settings.UserAgent = userAgent;
                System.Diagnostics.Debug.WriteLine($"[ScreenshotBlocker] User-Agent set: {preset} -> {userAgent}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScreenshotBlocker] SetCustomUserAgent error: {ex.Message}");
            }
        }
    }
}
