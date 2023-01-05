using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace ChromeWindowFix
{

    public struct AdjustRect
    {
        public int Top { get; set; }
        public int Bottom { get; set; }
    }

    public enum MatchConditionVariable { processName, mainWindowTitle }
    public enum MatchConditionComparison { contains }

    public class MatchCondition
    {
        public MatchConditionVariable Name { get; set; }
        public MatchConditionComparison Condition { get; set; }
        public string Value { get; set; } = "";
    }

    public class WindowPositionFixer
    {
        public string Name { get; set; } = "";

        public List<MatchCondition> Match { get; set; } = new List<MatchCondition>();

        public AdjustRect Adjust { get; set; }

        public void Run()
        {
            SystemParametersInfo( SystemParameters.SPI_GETWORKAREA, 0, out RECT tmp, 0 );
            var desktopRect = new Rectangle( tmp.Left, tmp.Top, tmp.Right - tmp.Left, tmp.Bottom - tmp.Top );

            foreach( var hwnd in Process.GetProcesses().Where( p => ShouldSelectProcess( p ) ).Select( p => p.MainWindowHandle ) )
            {
                DwmGetWindowAttribute( hwnd, DwmWindowAttribute.DWMWA_EXTENDED_FRAME_BOUNDS, out tmp, Marshal.SizeOf( typeof( RECT ) ) );
                var windowRect = new Rectangle( tmp.Left, tmp.Top, tmp.Right - tmp.Left, tmp.Bottom - tmp.Top );

                if( DoesWindowNeedFix( windowRect, desktopRect ) )
                    SetWindow( hwnd, windowRect, FixWindow( windowRect, desktopRect ) );
            }
        }

        private bool ShouldSelectProcess( Process p )
        {
            foreach( MatchCondition cond in Match )
            {
                string val = cond.Name switch {
                    MatchConditionVariable.mainWindowTitle => p.MainWindowTitle,
                    MatchConditionVariable.processName => p.ProcessName,
                    _ => ""
                };

                bool result = cond.Condition switch {
                    MatchConditionComparison.contains => val.Contains( cond.Value ),
                    _ => false
                };
                if( !result ) return false;
            }

            return true;
        }

        private bool DoesWindowNeedFix( Rectangle windowRect, Rectangle desktopRect ) =>
            !(windowRect.Left == desktopRect.Left && windowRect.Right == desktopRect.Right) &&
            (windowRect.Top > -10 && windowRect.Top < 10);

        private Rectangle FixWindow( Rectangle windowRect, Rectangle desktopRect ) =>
            new Rectangle( windowRect.X, Adjust.Top, windowRect.Width, desktopRect.Height - Adjust.Top );

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
