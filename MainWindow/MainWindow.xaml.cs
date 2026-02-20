using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Drawing;

namespace WinFlipped
{
    public partial class MainWindow : Window
    {
        private int _imageTop = 100;
        private int _imageLeft = 100;
        private int _verticalOffset = 10;
        private int _horizontalOffset = 10;
        // Rough approximation of maximum number of windows that this program can show
        private readonly int WINDOWS_SHOW_LIMIT = (int)Math.Min(
            SystemParameters.FullPrimaryScreenHeight / 100,
            SystemParameters.FullPrimaryScreenWidth / 150
        );
        private IEnumerable<(nint MainWindowHandle, string MainWindowTitle, Bitmap MainWindowScreenshot)>? OpenWindows;

        public MainWindow()
        {
            InitializeComponent();
            
            KeyUp += new KeyEventHandler(MainWindow_KeyUp);
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }
    }
}
