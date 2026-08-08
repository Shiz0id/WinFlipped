using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WinFlipped.Helpers;
using FormsContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using FormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using FormsToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;

namespace WinFlipped
{
    public partial class App : Application
    {
        private const int WmHotKey = 0x0312;
        private const int HotKeyId = 1;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private FormsNotifyIcon? _trayIcon;
        private HwndSource? _hotKeyWindow;
        private MainWindow? _mainWindow;
        private string? _pendingHotkeyFailureMessage;
        private ModifierKeys _modifiers = ModifierKeys.Control | ModifierKeys.Alt;
        private Key _key = Key.F;
        private readonly (string Label, ModifierKeys Modifiers, Key Key)[] _hotkeyOptions =
        [
            ("Ctrl + Alt + F", ModifierKeys.Control | ModifierKeys.Alt, Key.F),
            ("Ctrl + Alt + W", ModifierKeys.Control | ModifierKeys.Alt, Key.W),
            ("Ctrl + Shift + F", ModifierKeys.Control | ModifierKeys.Shift, Key.F)
        ];

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _mainWindow = new MainWindow();
            InitializeHotKeyWindow();
            InitializeTrayIcon();
            DebugLog.Write("Application started in tray mode.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_hotKeyWindow is not null)
            {
                UnregisterHotKey(_hotKeyWindow.Handle, HotKeyId);
                _hotKeyWindow.RemoveHook(WndProc);
                _hotKeyWindow.Dispose();
            }

            if (_trayIcon is not null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            DebugLog.Write("Application exiting.");
            base.OnExit(e);
        }

        private void InitializeHotKeyWindow()
        {
            _hotKeyWindow = new HwndSource(new HwndSourceParameters("WinFlippedHotKeyWindow"));
            _hotKeyWindow.AddHook(WndProc);

            if (!RegisterCurrentHotKey())
            {
                var hotkey = FormatHotkey(_modifiers, _key);
                DebugLog.Write($"Failed to register hotkey {hotkey}. Another application may already be using it.");
                _pendingHotkeyFailureMessage = $"Could not register hotkey {hotkey}. Use 'Rebind Hotkey' in the tray menu to choose another.";
            }
            else
            {
                DebugLog.Write($"Hotkey {FormatHotkey(_modifiers, _key)} registered successfully.");
            }
        }

        private void InitializeTrayIcon()
        {
            var contextMenu = new FormsContextMenuStrip();
            contextMenu.Items.Add("Show WinFlipped", null, (_, _) => ShowMainWindow());
            contextMenu.Items.Add("View Debug Log", null, (_, _) => OpenDebugLog());

            var hotkeyMenu = new FormsToolStripMenuItem("Rebind Hotkey");
            foreach (var option in _hotkeyOptions)
            {
                var menuItem = new FormsToolStripMenuItem(option.Label)
                {
                    Checked = option.Modifiers == _modifiers && option.Key == _key,
                    Tag = (option.Modifiers, option.Key)
                };
                menuItem.Click += (_, _) =>
                {
                    if (TrySetHotkey(option.Modifiers, option.Key))
                    {
                        UpdateHotkeyChecks(hotkeyMenu);
                    }
                };

                hotkeyMenu.DropDownItems.Add(menuItem);
            }

            contextMenu.Items.Add(hotkeyMenu);
            contextMenu.Items.Add("Quit", null, (_, _) => Shutdown());

            _trayIcon = new FormsNotifyIcon
            {
                Visible = true,
                Text = $"WinFlipped ({FormatHotkey(_modifiers, _key)})",
                Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty) ?? SystemIcons.Application,
                ContextMenuStrip = contextMenu
            };
            _trayIcon.DoubleClick += (_, _) => ShowMainWindow();

            if (_pendingHotkeyFailureMessage is not null)
            {
                _trayIcon.ShowBalloonTip(8000, "WinFlipped – Hotkey Unavailable", _pendingHotkeyFailureMessage, System.Windows.Forms.ToolTipIcon.Warning);
                _pendingHotkeyFailureMessage = null;
            }
            else
            {
                _trayIcon.ShowBalloonTip(4000, "WinFlipped is running", $"Press {FormatHotkey(_modifiers, _key)} or double-click this icon to open.", System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private static void OpenDebugLog()
        {
            var logPath = DebugLog.GetPath();
            var fileExisted = File.Exists(logPath);
            if (!fileExisted)
            {
                DebugLog.Write("Debug log file created.");
            }

            try
            {
                Process.Start(new ProcessStartInfo(logPath)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                DebugLog.Write($"Failed to open debug log: {exception.Message}");
            }
        }

        private bool RegisterCurrentHotKey()
        {
            if (_hotKeyWindow is null)
            {
                return false;
            }

            return RegisterHotKey(_hotKeyWindow.Handle, HotKeyId, (uint)_modifiers, (uint)KeyInterop.VirtualKeyFromKey(_key));
        }

        private bool TrySetHotkey(ModifierKeys modifiers, Key key)
        {
            if (_hotKeyWindow is null)
            {
                return false;
            }

            UnregisterHotKey(_hotKeyWindow.Handle, HotKeyId);

            var previousModifiers = _modifiers;
            var previousKey = _key;
            _modifiers = modifiers;
            _key = key;

            if (!RegisterCurrentHotKey())
            {
                _modifiers = previousModifiers;
                _key = previousKey;
                var rollbackSucceeded = RegisterCurrentHotKey();
                DebugLog.Write($"Hotkey rebind failed for {FormatHotkey(modifiers, key)}.");
                var balloonMessage = rollbackSucceeded
                    ? $"Could not register {FormatHotkey(modifiers, key)}. The previous hotkey is still active."
                    : $"Could not register {FormatHotkey(modifiers, key)}, and the previous hotkey could not be restored. Use 'Rebind Hotkey' to reassign.";
                _trayIcon?.ShowBalloonTip(6000, "WinFlipped – Rebind Failed", balloonMessage, System.Windows.Forms.ToolTipIcon.Warning);
                return false;
            }

            if (_trayIcon is not null)
            {
                _trayIcon.Text = $"WinFlipped ({FormatHotkey(_modifiers, _key)})";
            }

            DebugLog.Write($"Hotkey rebound to {FormatHotkey(_modifiers, _key)}.");

            return true;
        }

        private static string FormatHotkey(ModifierKeys modifiers, Key key)
        {
            var parts = new List<string>();
            if (modifiers.HasFlag(ModifierKeys.Control))
            {
                parts.Add("Ctrl");
            }

            if (modifiers.HasFlag(ModifierKeys.Alt))
            {
                parts.Add("Alt");
            }

            if (modifiers.HasFlag(ModifierKeys.Shift))
            {
                parts.Add("Shift");
            }

            parts.Add(key.ToString().ToUpperInvariant());
            return string.Join(" + ", parts);
        }

        private void UpdateHotkeyChecks(FormsToolStripMenuItem hotkeyMenu)
        {
            foreach (FormsToolStripMenuItem menuItem in hotkeyMenu.DropDownItems)
            {
                menuItem.Checked = menuItem.Tag is ValueTuple<ModifierKeys, Key> hotkey
                    && hotkey.Item1 == _modifiers
                    && hotkey.Item2 == _key;
            }
        }

        private void ShowMainWindow()
        {
            if (_mainWindow?.IsVisible == true)
            {
                DebugLog.Write("WinFlipped window already visible; ignoring re-summon.");
                _mainWindow.Activate();
                _mainWindow.Focus();
                return;
            }

            DebugLog.Write("Showing WinFlipped window.");
            _mainWindow?.SummonToForeground();
        }

        private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == WmHotKey && wParam.ToInt32() == HotKeyId)
            {
                ShowMainWindow();
                handled = true;
            }

            return 0;
        }
    }
}
