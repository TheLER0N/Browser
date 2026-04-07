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
    /// - Перехватывает сообщение WM_PRINTCLIENT (0x0318) через HwndSource.AddHook
    /// - WM_PRINTCLIENT отправляется когда другая программа пытается получить 
    ///   содержимое нашего окна через PrintWindow API
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
    /// </summary>
    public class SnippingToolBlocker : IDisposable
    {
        // === Сообщения Windows ===
        private const int WM_PRINT = 0x0317;
        private const int WM_PRINTCLIENT = 0x0318;
        private const int PRF_CLIENT = 0x00000004;
        private const int PRF_NONCLIENT = 0x00000020;

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
        /// Перехватывает WM_PRINTCLIENT и отклоняет запрос на рендеринг.
        /// </summary>
        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!_isEnabled) return IntPtr.Zero;

            // WM_PRINTCLIENT — запрос на отрисовку клиентской области
            // (Snipping Tool, PrintWindow API, и т.д.)
            if (msg == WM_PRINTCLIENT)
            {
                // Возвращаем NULL — окно не рендерится в буфер запросителя
                handled = true;
                return IntPtr.Zero;
            }

            // WM_PRINT — запрос на отрисовку всего окна
            if (msg == WM_PRINT)
            {
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
