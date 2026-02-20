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
            var imageTop = 100;
            var imageLeft = 100;
            var scale = 1.0;
            var zIndex = 1;
            var wasVisible = IsVisible;
            if (wasVisible)
            {
                Hide();
            }
            
            canvas.Children.Clear();

            OpenWindows = WindowsEnumerator.GetOpenWindows().Where(win => win.MainWindowHandle != new WindowInteropHelper(this).Handle);

            foreach ((nint mainWindowHandle, string mainWindowTitle, Bitmap mainWindowScreenshot) in OpenWindows.TakeLast(WINDOWS_SHOW_LIMIT))
            {
                canvas.DrawWindow(mainWindowHandle, mainWindowTitle, mainWindowScreenshot, imageTop, imageLeft, scale, zIndex);

                // Change position of subsequent window
                imageLeft += 10;
                imageTop += 10;
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
