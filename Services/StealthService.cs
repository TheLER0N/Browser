using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис режима невидимости — скрывает окно от захвата экрана через
    /// SetWindowDisplayAffinity (Windows Graphics Capture API).
    /// 
    /// Как это работает:
    /// - WDA_EXCLUDEFROMCAPTURE (0x11): окно полностью исчезает из захвата
    ///   (OBS Game Capture, Discord Screen Share, Zoom). Работает на Windows 10 2004+.
    /// - WDA_MONITOR (0x01): fallback для старых версий Windows — окно видно
    ///   только на физическом мониторе, не захватывается виртуальными драйверами.
    /// - WDA_NONE (0x00): обычное поведение — окно видно всем.
    /// 
    /// Ограничения:
    /// - Не защищает от захвата через захват фреймбуфера (например, OBS Display Capture)
    /// - Не шифрует данные — только скрывает окно
    /// - При сворачивании окно может стать видимым (зависит от версии Windows)
    /// </summary>
    public class StealthService
    {
        // === Window Display Affinity constants ===
        // WDA_NONE: Сбросить защиту — окно видно всем
        private const uint WDA_NONE = 0x00000000;
        // WDA_MONITOR: Видно только на физическом мониторе
        private const uint WDA_MONITOR = 0x00000001;
        // WDA_EXCLUDEFROMCAPTURE: Полностью скрыть от захвата (Win 10 2004+)
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);

        private Window? _window;
        private IntPtr _hWnd;
        private bool _isStealthMode;
        private bool _pendingStealthMode;
        private bool _stealthModeRequested;

        public bool IsStealthMode => _isStealthMode;

        /// <summary>
        /// Событие изменения режима невидимости.
        /// </summary>
        public event EventHandler<bool>? StealthModeChanged;

        /// <summary>
        /// Инициализирует сервис. Должен вызываться один раз при создании окна.
        /// Подписывается на SourceInitialized — момент, когда оконный хэндл становится доступен.
        /// </summary>
        public void Initialize(Window window)
        {
            _window = window;
            _window.SourceInitialized += OnSourceInitialized;
        }

        /// <summary>
        /// Обработчик SourceInitialized — хэндл окна готов, можно применять affinity.
        /// Если запрос на stealth mode пришёл раньше (до инициализации), применяем его сейчас.
        /// </summary>
        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            _hWnd = new WindowInteropHelper(_window!).Handle;

            // Применяем отложенный запрос на stealth mode
            if (_stealthModeRequested)
            {
                ApplyAffinity(_pendingStealthMode);
                _stealthModeRequested = false;
            }
        }

        /// <summary>
        /// Переключает режим невидимости на противоположную.
        /// </summary>
        public void ToggleStealthMode()
        {
            SetStealthMode(!_isStealthMode);
        }

        /// <summary>
        /// Устанавливает конкретный режим невидимости.
        /// Если оконный хэндл ещё не готов — запоминаем запрос и применим позже.
        /// </summary>
        /// <param name="enabled">true — скрыть от захвата, false — показать</param>
        public void SetStealthMode(bool enabled)
        {
            if (_isStealthMode == enabled) return;

            if (_hWnd == IntPtr.Zero)
            {
                // Окно ещё не инициализировано, запоминаем запрос
                _stealthModeRequested = true;
                _pendingStealthMode = enabled;
                return;
            }

            ApplyAffinity(enabled);
        }

        /// <summary>
        /// Применяет window display affinity через Win32 API.
        /// Если WDA_EXCLUDEFROMCAPTURE не поддерживается (старая Windows),
        /// fallback на WDA_MONITOR.
        /// </summary>
        private void ApplyAffinity(bool enabled)
        {
            _isStealthMode = enabled;

            // WDA_EXCLUDEFROMCAPTURE — окно полностью исчезает из захвата (OBS, Discord).
            // Работает на Windows 10 2004+. На старых версиях fallback на WDA_MONITOR.
            var affinity = enabled ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE;
            bool success = SetWindowDisplayAffinity(_hWnd, affinity);

            // Fallback для старых версий Windows
            if (!success && enabled)
            {
                affinity = WDA_MONITOR;
                success = SetWindowDisplayAffinity(_hWnd, affinity);
            }

            if (!success)
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine(
                    $"SetWindowDisplayAffinity failed. Error code: {error}");
            }

            StealthModeChanged?.Invoke(this, _isStealthMode);
        }

        /// <summary>
        /// Проверяет текущий affinity окна.
        /// </summary>
        public uint GetCurrentAffinity()
        {
            if (_hWnd == IntPtr.Zero) return 0;
            GetWindowDisplayAffinity(_hWnd, out var affinity);
            return affinity;
        }

        public void Dispose()
        {
            if (_window != null)
            {
                _window.SourceInitialized -= OnSourceInitialized;
            }

            // Сбрасываем режим при уничтожении
            if (_hWnd != IntPtr.Zero && _isStealthMode)
            {
                SetWindowDisplayAffinity(_hWnd, WDA_NONE);
                _isStealthMode = false;
            }
        }
    }
}
