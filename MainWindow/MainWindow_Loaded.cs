using System.Windows.Interop;
using System.Windows;
using WinFlipped.Helpers;
using Bitmap = System.Drawing.Bitmap;

namespace WinFlipped
{
    public partial class MainWindow : Window
    {
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RenderWindows();
        }

        private void RenderWindows()
        {
            var scale = 1.0;
            var zIndex = 1;
            var wasVisible = IsVisible;
            if (wasVisible)
            {
                Hide();
            }
            
            canvas.Children.Clear();

            OpenWindows = WindowsEnumerator.GetOpenWindows().Where(win => win.MainWindowHandle != new WindowInteropHelper(this).Handle);
            var visibleWindows = OpenWindows.TakeLast(WINDOWS_SHOW_LIMIT).ToList();
            var visibleWindowsCount = Math.Max(1, visibleWindows.Count);

            _horizontalOffset = Math.Max(10, (int)((SystemParameters.VirtualScreenWidth - 150) / visibleWindowsCount));
            _verticalOffset = Math.Max(10, (int)((SystemParameters.VirtualScreenHeight - 100) / visibleWindowsCount));
            _imageLeft = Math.Max(0, (int)((SystemParameters.VirtualScreenWidth - (_horizontalOffset * (visibleWindowsCount - 1) + 150)) / 2));
            _imageTop = Math.Max(0, (int)((SystemParameters.VirtualScreenHeight - (_verticalOffset * (visibleWindowsCount - 1) + 100)) / 2));

            var imageTop = _imageTop;
            var imageLeft = _imageLeft;
            foreach ((nint mainWindowHandle, string mainWindowTitle, Bitmap mainWindowScreenshot) in visibleWindows)
            {
                canvas.DrawWindow(mainWindowHandle, mainWindowTitle, mainWindowScreenshot, imageTop, imageLeft, scale, zIndex);

                // Change position of subsequent window
                imageLeft += _horizontalOffset;
                imageTop += _verticalOffset;
                scale += 0.1;
                zIndex++;
            }

            if (wasVisible)
            {
                Show();
            }

            Activate();
        }
    }
}
