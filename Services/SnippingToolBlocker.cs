using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис блокировки скриншотов от Snipping Tool и подобных утилит.
    ///
    /// Как это работает:
    /// - Перехватывает сообщения WM_PRINTCLIENT, WM_PRINT, WM_DWMSENDICONICTHUMBNAIL,
    ///   WM_DWMSENDICONICLIVEPREVIEWBITMAP через HwndSource.AddHook
    /// - Эти сообщения отправляются когда другая программа пытается получить
    ///   содержимое нашего окна через PrintWindow API или DWM
    /// - Snipping Tool, Lightshot и подобные используют PrintWindow
    /// - Возвращаем FALSE — окно не рендерится в буфер скриншота
    ///
    /// В комбинации с WDA_EXCLUDEFROMCAPTURE (StealthService) это обеспечивает
    /// полную защиту от всех типов скриншотов.
    ///
    /// Ограничения:
    /// - Не защищает от захвата через виртуальные драйверы дисплея
    /// - Не защищает от физической камеры (фото монитора)
    /// - Некоторые программы могут использовать другие методы захвата
    /// - НЕ защищает от драйверов уровня ядра (Net Control)
    /// </summary>
    public class SnippingToolBlocker : IDisposable
    {
        // === Сообщения Windows ===
        private const int WM_PRINT = 0x0317;
        private const int WM_PRINTCLIENT = 0x0318;
        private const int PRF_CLIENT = 0x00000004;
        private const int PRF_NONCLIENT = 0x00000020;
        
        // === DWM сообщения для Windows 10+ ===
        private const int WM_DWMSENDICONICTHUMBNAIL = 0x0323;
        private const int WM_DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326;
        private const int WM_DWMWINDOWMAXIMIZEDCHANGE = 0x0328;
        
        // === Дополнительные сообщения для перехвата ===
        private const int WM_CAP = 0x0400; // Capture сообщения
        private const int WM_GETTEXT = 0x000D; // Запрос текста окна
        private const int WM_GETTEXTLENGTH = 0x000E; // Запрос длины текста

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        private Window? _window;
        private IntPtr _hWnd;
        private HwndSource? _hwndSource;
        private bool _isEnabled;

        /// <summary>
        /// Включена ли блокировка Snipping Tool.
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Инициализирует сервис и подписывается на сообщения окна.
        /// </summary>
        public void Initialize(Window window)
        {
            _window = window;
            _window.SourceInitialized += OnSourceInitialized;
        }

        /// <summary>
        /// Обработчик SourceInitialized — хэндл окна готов.
        /// </summary>
        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            _hWnd = new WindowInteropHelper(_window!).Handle;
            _hwndSource = HwndSource.FromHwnd(_hWnd);
            _hwndSource?.AddHook(WndProc);

            // Если блокировка была включена до инициализации — применяем
            if (_isEnabled)
            {
                EnableBlocking();
            }
        }

        /// <summary>
        /// Обработчик оконных сообщений.
        /// Перехватывает WM_PRINTCLIENT, WM_PRINT, DWM сообщения и отклоняет запросы на рендеринг.
        /// </summary>
        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!_isEnabled) return IntPtr.Zero;

            switch (msg)
            {
                // === Основные сообщения захвата ===
                case WM_PRINTCLIENT:
                    // Запрос на отрисовку клиентской области
                    // (Snipping Tool, PrintWindow API, и т.д.)
                    handled = true;
                    return IntPtr.Zero;

                case WM_PRINT:
                    // Запрос на отрисовку всего окна
                    handled = true;
                    return IntPtr.Zero;

                // === DWM сообщения (Windows 10+) ===
                case WM_DWMSENDICONICTHUMBNAIL:
                    // Запрос миниатюры окна для панели задач/Alt+Tab
                    handled = true;
                    return IntPtr.Zero;

                case WM_DWMSENDICONICLIVEPREVIEWBITMAP:
                    // Запрос live превью при наведении на панель задач
                    handled = true;
                    return IntPtr.Zero;

                case WM_DWMWINDOWMAXIMIZEDCHANGE:
                    // Изменение состояния максимизации
                    // Возвращаем пустой результат
                    handled = true;
                    return IntPtr.Zero;

                // === Дополнительные сообщения захвата ===
                case WM_CAP:
                    // Capture сообщения (используются некоторыми программами захвата)
                    handled = true;
                    return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Включает блокировку Snipping Tool.
        /// </summary>
        public void EnableBlocking()
        {
            _isEnabled = true;
            System.Diagnostics.Debug.WriteLine("[SnippingToolBlocker] Blocking enabled");
        }

        /// <summary>
        /// Выключает блокировку Snipping Tool.
        /// </summary>
        public void DisableBlocking()
        {
            _isEnabled = false;
            System.Diagnostics.Debug.WriteLine("[SnippingToolBlocker] Blocking disabled");
        }

        /// <summary>
        /// Переключает состояние блокировки.
        /// </summary>
        public void ToggleBlocking()
        {
            if (_isEnabled)
                DisableBlocking();
            else
                EnableBlocking();
        }

        public void Dispose()
        {
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource.Dispose();
                _hwndSource = null;
            }

            if (_window != null)
            {
                _window.SourceInitialized -= OnSourceInitialized;
                _window = null;
            }

            _hWnd = IntPtr.Zero;
            _isEnabled = false;
        }
    }
}
