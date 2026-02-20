using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
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

            base.OnExit(e);
        }

        private void InitializeHotKeyWindow()
        {
            _hotKeyWindow = new HwndSource(new HwndSourceParameters("WinFlippedHotKeyWindow"));
            _hotKeyWindow.AddHook(WndProc);
            RegisterCurrentHotKey();
        }

        private void InitializeTrayIcon()
        {
            var contextMenu = new FormsContextMenuStrip();
            contextMenu.Items.Add("Show WinFlipped", null, (_, _) => ShowMainWindow());

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
                RegisterCurrentHotKey();
                return false;
            }

            if (_trayIcon is not null)
            {
                _trayIcon.Text = $"WinFlipped ({FormatHotkey(_modifiers, _key)})";
            }

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
