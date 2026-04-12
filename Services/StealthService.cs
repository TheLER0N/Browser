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
    /// - НЕ защищает от драйверов уровня ядра (Net Control, системы администрирования)
    /// </summary>
    public class StealthService
    {
        // ═══════════════════════════════════════════
        // Win32 API для проверки версии Windows
        // ═══════════════════════════════════════════
        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct RTL_OSVERSIONINFOEX
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }

        // ═══════════════════════════════════════════
        // Window Display Affinity constants
        // ═══════════════════════════════════════════
        // WDA_NONE: Сбросить защиту — окно видно всем
        private const uint WDA_NONE = 0x00000000;
        // WDA_MONITOR: Видно только на физическом мониторе
        private const uint WDA_MONITOR = 0x00000001;
        // WDA_EXCLUDEFROMCAPTURE: Полностью скрыть от захвата (Win 10 2004+)
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        // WDA_EXCLUDEFROMCAPTURE для старых версий (может не работать)
        private const uint WDA_EXCLUDEFROMCAPTURE_LEGACY = 0x00000003;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        private Window? _window;
        private IntPtr _hWnd;
        private bool _isStealthMode;
        private bool _pendingStealthMode;
        private bool _stealthModeRequested;
        private bool _isWin10_2004OrLater;

        public bool IsStealthMode => _isStealthMode;

        /// <summary>
        /// Событие изменения режима невидимости.
        /// </summary>
        public event EventHandler<bool>? StealthModeChanged;

        public StealthService()
        {
            // Проверяем версию Windows при создании сервиса
            _isWin10_2004OrLater = IsWindows10_2004OrLater();
            System.Diagnostics.Debug.WriteLine($"[StealthService] Windows 10 2004+: {_isWin10_2004OrLater}");
        }

        /// <summary>
        /// Проверяет, является ли версия Windows 10 2004 (build 19041) или новее.
        /// </summary>
        private bool IsWindows10_2004OrLater()
        {
            try
            {
                var osInfo = new RTL_OSVERSIONINFOEX();
                osInfo.dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(RTL_OSVERSIONINFOEX));
                
                if (RtlGetVersion(out osInfo) == 0)
                {
                    // Windows 10 2004 = build 19041
                    bool isRecentEnough = osInfo.dwMajorVersion > 10 || 
                        (osInfo.dwMajorVersion == 10 && osInfo.dwBuildNumber >= 19041);
                    
                    System.Diagnostics.Debug.WriteLine(
                        $"[StealthService] Windows version: {osInfo.dwMajorVersion}.{osInfo.dwMinorVersion} " +
                        $"(build {osInfo.dwBuildNumber})");
                    
                    return isRecentEnough;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StealthService] Version check error: {ex.Message}");
            }
            
            // По умолчанию считаем что версия достаточная
            return true;
        }

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
        /// Использует проверку версии Windows для выбора правильной константы.
        /// </summary>
        private void ApplyAffinity(bool enabled)
        {
            _isStealthMode = enabled;

            if (!enabled)
            {
                // Отключаем защиту
                bool success = SetWindowDisplayAffinity(_hWnd, WDA_NONE);
                if (!success)
                {
                    var error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine(
                        $"[StealthService] SetWindowDisplayAffinity(WDA_NONE) failed. Error: {error}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[StealthService] Stealth mode DISABLED");
                }
                StealthModeChanged?.Invoke(this, _isStealthMode);
                return;
            }

            // === ВКЛЮЧАЕМ ЗАЩИТУ ===
            
            // Стратегия 1: WDA_EXCLUDEFROMCAPTURE (только Win 10 2004+)
            if (_isWin10_2004OrLater)
            {
                bool success = SetWindowDisplayAffinity(_hWnd, WDA_EXCLUDEFROMCAPTURE);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("[StealthService] Stealth mode ENABLED (WDA_EXCLUDEFROMCAPTURE)");
                    StealthModeChanged?.Invoke(this, _isStealthMode);
                    return;
                }
            }

            // Стратегия 2: Fallback на WDA_MONITOR
            System.Diagnostics.Debug.WriteLine("[StealthService] Fallback to WDA_MONITOR");
            bool success2 = SetWindowDisplayAffinity(_hWnd, WDA_MONITOR);
            
            if (!success2)
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine(
                    $"[StealthService] All affinity methods failed. Error: {error}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[StealthService] Stealth mode ENABLED (WDA_MONITOR fallback)");
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
