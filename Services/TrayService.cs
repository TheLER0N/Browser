using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using GhostBrowser.Services;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис системного трея — сворачивание окна в трей, 
    /// восстановление по двойному клику, контекстное меню.
    /// </summary>
    public class TrayService : IDisposable
    {
        private NotifyIcon? _trayIcon;
        private Window? _window;
        private StealthService? _stealthService;
        private bool _isMinimizedToTray;

        /// <summary>
        /// Инициализирует сервис трея.
        /// </summary>
        public void Initialize(Window window, StealthService stealthService)
        {
            _window = window;
            _stealthService = stealthService;

            // Создаём иконку в трее
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Google Chrome",
                Visible = true
            };

            // Двойной клик — восстановление окна
            _trayIcon.MouseDoubleClick += TrayIcon_MouseDoubleClick;

            // Контекстное меню
            var contextMenu = new ContextMenuStrip();
            
            var openItem = contextMenu.Items.Add("Открыть");
            openItem.Click += (s, e) => RestoreFromTray();

            var stealthItem = contextMenu.Items.Add("Stealth Mode: OFF");
            stealthItem.Click += (s, e) =>
            {
                _stealthService?.ToggleStealthMode();
                UpdateStealthMenuItem(stealthItem);
            };

            contextMenu.Items.Add("-"); // Разделитель

            var exitItem = contextMenu.Items.Add("Выход");
            exitItem.Click += (s, e) => _window?.Close();

            _trayIcon.ContextMenuStrip = contextMenu;

            System.Diagnostics.Debug.WriteLine("[TrayService] Initialized");
        }

        /// <summary>
        /// Сворачивает окно в трей.
        /// </summary>
        public void MinimizeToTray()
        {
            if (_window == null) return;

            _isMinimizedToTray = true;
            _window.Hide();
            System.Diagnostics.Debug.WriteLine("[TrayService] Minimized to tray");
        }

        /// <summary>
        /// Восстанавливает окно из трея.
        /// </summary>
        public void RestoreFromTray()
        {
            if (_window == null) return;

            _isMinimizedToTray = false;
            _window.Show();
            _window.WindowState = WindowState.Normal;
            _window.Activate();
            System.Diagnostics.Debug.WriteLine("[TrayService] Restored from tray");
        }

        /// <summary>
        /// Переключает состояние: свернуть/восстановить.
        /// </summary>
        public void ToggleTray()
        {
            if (_isMinimizedToTray)
                RestoreFromTray();
            else
                MinimizeToTray();
        }

        /// <summary>
        /// Возвращает true, если окно свёрнуто в трей.
        /// </summary>
        public bool IsMinimizedToTray => _isMinimizedToTray;

        private void TrayIcon_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                RestoreFromTray();
            }
        }

        private void UpdateStealthMenuItem(ToolStripItem item)
        {
            if (_stealthService != null)
            {
                item.Text = _stealthService.IsStealthMode ? "Stealth Mode: ON" : "Stealth Mode: OFF";
            }
        }

        /// <summary>
        /// Обновляет иконку в трее при изменении stealth режима.
        /// </summary>
        public void UpdateStealthIndicator(bool isStealth)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Text = isStealth ? "Google Chrome (Stealth ON)" : "Google Chrome";
            }
        }

        public void Dispose()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
    }
}
