using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Threading;

namespace ChromeWindowFix
{
    internal class WindowPositionFixer
    {
        private Func<Process, bool> processSelector;
        private Func<Rectangle, Rectangle, bool> windowNeedsFixSelector;
        private Func<Rectangle, Rectangle> fixWindow;

        private DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds( 1 ) };

        public WindowPositionFixer( Func<Process, bool> processSelector, Func<Rectangle, Rectangle, bool> windowNeedsFixSelector,
            Func<Rectangle, Rectangle> fixWindow )
        {
            this.processSelector = processSelector;
            this.windowNeedsFixSelector = windowNeedsFixSelector;
            this.fixWindow = fixWindow;

            timer.Tick += (s,e) => FixWindows();
            timer.Start();
        }

        private void FixWindows()
        {
            SystemParametersInfo( SystemParameters.SPI_GETWORKAREA, 0, out RECT tmp, 0 );
            var desktopRect = new Rectangle( tmp.Left, tmp.Top, tmp.Right - tmp.Left, tmp.Bottom - tmp.Top );

            foreach( var hwnd in Process.GetProcesses().Where( p => processSelector( p ) ).Select( p => p.MainWindowHandle ) )
            {
                DwmGetWindowAttribute( hwnd, DwmWindowAttribute.DWMWA_EXTENDED_FRAME_BOUNDS, out tmp, Marshal.SizeOf( typeof( RECT ) ) );
                var windowRect = new Rectangle( tmp.Left, tmp.Top, tmp.Right - tmp.Left, tmp.Bottom - tmp.Top );

                if( windowNeedsFixSelector( windowRect, desktopRect ) )
                    SetWindow( hwnd, windowRect, fixWindow( windowRect ) );
            }
        }

        public static void SetWindow( IntPtr hwnd, Rectangle originalRect, Rectangle newRect )
        {
            GetWindowRect( hwnd, out RECT originalWithShadow );

            var shadow = new RECT {
                Left = originalWithShadow.Left - originalRect.Left,
                Right = originalWithShadow.Right - originalRect.Right,
                Top = originalWithShadow.Top - originalRect.Top,
                Bottom = originalWithShadow.Bottom - originalRect.Bottom
            };

            int width = (newRect.Right + shadow.Right) - (newRect.Left + shadow.Left);
            int height = (newRect.Bottom + shadow.Bottom) - (newRect.Top - shadow.Top);

            SetWindowPos( hwnd, IntPtr.Zero, newRect.Left + shadow.Left, newRect.Top + shadow.Top, width, height, 0x400 );
        }

        [DllImport( "dwmapi.dll" )]
        static extern int DwmGetWindowAttribute( IntPtr hWnd, DwmWindowAttribute dwAttribute, out RECT lpRect, int cbAttribute );
        
        [StructLayout( LayoutKind.Sequential )]
        public struct RECT { public int Left, Top, Right, Bottom; }

        public enum DwmWindowAttribute { DWMWA_EXTENDED_FRAME_BOUNDS = 9 }

        [DllImport( "user32.dll" )]
        static extern bool SystemParametersInfo( SystemParameters uiAction, uint uiParam, out RECT pvParam, uint fWinIni );

        enum SystemParameters { SPI_GETWORKAREA = 0x0030 };

        [DllImport( "user32.dll" )]
        internal static extern void GetWindowRect( IntPtr hwnd, out RECT lpRect );

        [DllImport( "user32.dll" )]
        internal static extern void SetWindowPos( IntPtr hwnd, IntPtr hwndInsertAfter, int X, int Y, int nWidth, int nHeight, uint flags );
    }
}
