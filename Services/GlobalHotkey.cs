using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис глобальных горячих клавиш — перехватывает системные комбинации клавиш
    /// до того, как они достигнут других приложений.
    ///
    /// Используется для блокировки PrintScreen и других системных горячих клавиш.
    ///
    /// Как это работает:
    /// - RegisterHotKey регистрирует глобальный хук на уровне Windows
    /// - Когда пользователь нажимает зарегистрированную комбинацию, Windows
    ///   отправляет сообщение WM_HOTKEY нашему приложению
    /// - Другие приложения НЕ получают это событие (оно перехвачено)
    /// - При закрытии приложения хуки снимаются через UnregisterHotKey
    ///
    /// Ограничения:
    /// - Работает только пока приложение запущено
    /// - Некоторые комбинации (Ctrl+Alt+Del) НЕЛЬЗЯ перехватить
    /// - При сворачивании окна хуки всё ещё работают (глобальные)
    /// 
    /// Паник-кнопка: Ctrl+0 (ID=100)
    /// </summary>
    public class GlobalHotkey : IDisposable
    {
        // === Win32 API для глобальных горячих клавиш ===
        
        /// <summary>
        /// Регистрирует глобальную горячую клавишу.
        /// </summary>
        /// <param name="hWnd">Хэндл окна-получателя сообщений WM_HOTKEY</param>
        /// <param name="id">Уникальный ID хоткея (для последующего удаления)</param>
        /// <param name="fsModifiers">Модификаторы: 0 = нет, MOD_CONTROL = 0x0002, MOD_ALT = 0x0001, MOD_SHIFT = 0x0004</param>
        /// <param name="vk">Virtual-key code (например, VK_SNAPSHOT = 0x2C для PrintScreen)</param>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        /// <summary>
        /// Снимает глобальную горячую клавишу.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // === Virtual-key codes ===
        private const uint VK_SNAPSHOT = 0x2C;           // PrintScreen
        private const uint VK_ESCAPE = 0x1B;             // Escape
        private const uint VK_0 = 0x30;                  // 0 — Паник-кнопка (Ctrl+0)
        private const uint VK_OEM_3 = 0xC0;              // ` (тильда/ё) — Восстановление из трея (Ctrl+`)

        // === Модификаторы ===
        private const uint MOD_NOREPEAT = 0x4000;        // Не повторять при удержании
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_SHIFT = 0x0004;

        // === Сообщение WM_HOTKEY ===
        private const int WM_HOTKEY = 0x0312;

        private Window? _window;
        private IntPtr _hWnd;
        private HwndSource? _hwndSource;
        private bool _isBlockingEnabled;
        private bool _isPanicKeyEnabled;
        private bool _isRestoreFromTrayKeyEnabled;

        /// <summary>
        /// Событие срабатывания горячей клавиши.
        /// </summary>
        public event EventHandler<int>? HotKeyPressed;

        /// <summary>
        /// Включена ли блокировка PrintScreen.
        /// </summary>
        public bool IsBlockingEnabled => _isBlockingEnabled;

        /// <summary>
        /// Включена ли паник-кнопка Ctrl+0.
        /// </summary>
        public bool IsPanicKeyEnabled => _isPanicKeyEnabled;

        /// <summary>
        /// Включена ли клавиша восстановления из трея Ctrl+`.
        /// </summary>
        public bool IsRestoreFromTrayKeyEnabled => _isRestoreFromTrayKeyEnabled;

        /// <summary>
        /// Инициализирует сервис и подписывается на сообщения окна.
        /// Должен вызываться один раз при создании окна.
        /// </summary>
        public void Initialize(Window window)
        {
            _window = window;
            _window.SourceInitialized += OnSourceInitialized;
        }

        /// <summary>
        /// Обработчик SourceInitialized — хэндл окна готов, можно перехватывать сообщения.
        /// </summary>
        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            _hWnd = new WindowInteropHelper(_window!).Handle;
            _hwndSource = HwndSource.FromHwnd(_hWnd);
            _hwndSource?.AddHook(WndProc);

            // Если блокировка была включена до инициализации — применяем сейчас
            if (_isBlockingEnabled)
            {
                RegisterAllHotkeys();
            }

            // Если паник-кнопка была включена до инициализации — применяем сейчас
            if (_isPanicKeyEnabled)
            {
                RegisterPanicKey();
            }
        }

        /// <summary>
        /// Обработчик оконных сообщений.
        /// Перехватывает WM_HOTKEY и генерирует событие HotKeyPressed.
        /// </summary>
        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                HotKeyPressed?.Invoke(this, hotkeyId);
                handled = true; // Помечаем как обработанное — другие обработчики не получат
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Включает блокировку PrintScreen.
        /// Регистрирует глобальные хуки на:
        /// - PrintScreen (0x2C)
        /// - Ctrl+PrintScreen
        /// - Alt+PrintScreen
        /// </summary>
        public void EnableBlocking()
        {
            if (_isBlockingEnabled) return;
            _isBlockingEnabled = true;

            // Если окно ещё не инициализировано — хуки зарегистрируются позже в OnSourceInitialized
            if (_hWnd == IntPtr.Zero) return;

            RegisterAllHotkeys();
        }

        /// <summary>
        /// Выключает блокировку PrintScreen.
        /// Снимает все зарегистрированные хуки.
        /// </summary>
        public void DisableBlocking()
        {
            if (!_isBlockingEnabled) return;
            _isBlockingEnabled = false;

            UnregisterAllHotkeys();
        }

        /// <summary>
        /// Переключает состояние блокировки.
        /// </summary>
        public void ToggleBlocking()
        {
            if (_isBlockingEnabled)
                DisableBlocking();
            else
                EnableBlocking();
        }

        /// <summary>
        /// Регистрирует все хоткеи для блокировки PrintScreen.
        /// ID хоткеев: 1 = PrintScreen, 2 = Ctrl+PrintScreen, 3 = Alt+PrintScreen
        /// </summary>
        private void RegisterAllHotkeys()
        {
            if (_hWnd == IntPtr.Zero) return;

            // PrintScreen (без модификаторов)
            bool success1 = RegisterHotKey(_hWnd, 1, MOD_NOREPEAT, VK_SNAPSHOT);
            
            // Ctrl+PrintScreen
            bool success2 = RegisterHotKey(_hWnd, 2, MOD_CONTROL | MOD_NOREPEAT, VK_SNAPSHOT);
            
            // Alt+PrintScreen
            bool success3 = RegisterHotKey(_hWnd, 3, MOD_ALT | MOD_NOREPEAT, VK_SNAPSHOT);

            if (!success1 || !success2 || !success3)
            {
                var error1 = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine(
                    $"RegisterHotKey failed for some keys. Error codes: {error1}. " +
                    $"PrtScn: {success1}, Ctrl+PrtScn: {success2}, Alt+PrtScn: {success3}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PrintScreen blocking enabled successfully");
            }
        }

        /// <summary>
        /// Снимает все зарегистрированные хоткеи.
        /// </summary>
        private void UnregisterAllHotkeys()
        {
            if (_hWnd == IntPtr.Zero) return;

            UnregisterHotKey(_hWnd, 1);
            UnregisterHotKey(_hWnd, 2);
            UnregisterHotKey(_hWnd, 3);

            System.Diagnostics.Debug.WriteLine("PrintScreen blocking disabled");
        }

        /// <summary>
        /// Проверяет, является ли текущая нажатая клавиша PrintScreen.
        /// Вызывается из MainWindow.OnKeyDown для дополнительной защиты.
        /// </summary>
        public static bool IsPrintScreenKey(Key key)
        {
            return key == Key.Snapshot || key == Key.PrintScreen;
        }

        // ═══════════════════════════════════════════
        // ПАНИК-КНОПКА (F12)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Включает паник-кнопку F12.
        /// При нажатии F12 генерируется событие HotKeyPressed с ID = 100.
        /// </summary>
        public void EnablePanicKey()
        {
            if (_isPanicKeyEnabled) return;
            _isPanicKeyEnabled = true;

            if (_hWnd == IntPtr.Zero) return;
            RegisterPanicKey();
        }

        /// <summary>
        /// Выключает паник-кнопку Ctrl+0.
        /// </summary>
        public void DisablePanicKey()
        {
            if (!_isPanicKeyEnabled) return;
            _isPanicKeyEnabled = false;

            UnregisterPanicKey();
        }

        /// <summary>
        /// Переключает состояние паник-кнопки.
        /// </summary>
        public void TogglePanicKey()
        {
            if (_isPanicKeyEnabled)
                DisablePanicKey();
            else
                EnablePanicKey();
        }

        /// <summary>
        /// Регистрирует хоткей Ctrl+0 (ID = 100).
        /// </summary>
        private void RegisterPanicKey()
        {
            if (_hWnd == IntPtr.Zero) return;

            bool success = RegisterHotKey(_hWnd, 100, MOD_CONTROL | MOD_NOREPEAT, VK_0);
            if (!success)
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"RegisterHotKey Ctrl+0 failed. Error: {error}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Panic key (Ctrl+0) enabled");
            }
        }

        /// <summary>
        /// Снимает хоткей Ctrl+0.
        /// </summary>
        private void UnregisterPanicKey()
        {
            if (_hWnd == IntPtr.Zero) return;
            UnregisterHotKey(_hWnd, 100);
            System.Diagnostics.Debug.WriteLine("Panic key (Ctrl+0) disabled");
        }

        // ═══════════════════════════════════════════
        // ВОССТАНОВЛЕНИЕ ИЗ ТРЕЯ (Ctrl+`)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Включает клавишу восстановления из трея Ctrl+`.
        /// При нажатии Ctrl+` генерируется событие HotKeyPressed с ID = 200.
        /// </summary>
        public void EnableRestoreFromTrayKey()
        {
            if (_isRestoreFromTrayKeyEnabled) return;
            _isRestoreFromTrayKeyEnabled = true;
            if (_hWnd == IntPtr.Zero) return;
            RegisterRestoreFromTrayKey();
        }

        /// <summary>
        /// Выключает клавишу восстановления из трея Ctrl+`.
        /// </summary>
        public void DisableRestoreFromTrayKey()
        {
            if (!_isRestoreFromTrayKeyEnabled) return;
            _isRestoreFromTrayKeyEnabled = false;
            UnregisterRestoreFromTrayKey();
        }

        /// <summary>
        /// Регистрирует хоткей Ctrl+` (ID = 200).
        /// </summary>
        private void RegisterRestoreFromTrayKey()
        {
            if (_hWnd == IntPtr.Zero) return;

            bool success = RegisterHotKey(_hWnd, 200, MOD_CONTROL | MOD_NOREPEAT, VK_OEM_3);
            if (!success)
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"RegisterHotKey Ctrl+` failed. Error: {error}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Restore from tray key (Ctrl+`) enabled");
            }
        }

        /// <summary>
        /// Снимает хоткей Ctrl+`.
        /// </summary>
        private void UnregisterRestoreFromTrayKey()
        {
            if (_hWnd == IntPtr.Zero) return;
            UnregisterHotKey(_hWnd, 200);
            System.Diagnostics.Debug.WriteLine("Restore from tray key (Ctrl+`) disabled");
        }

        public void Dispose()
        {
            UnregisterAllHotkeys();
            UnregisterPanicKey();
            UnregisterRestoreFromTrayKey();

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
            _isBlockingEnabled = false;
            _isPanicKeyEnabled = false;
            _isRestoreFromTrayKeyEnabled = false;
        }
    }
}
