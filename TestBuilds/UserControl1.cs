using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers    
// ReSharper disable once UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace TestBuilds {                       
    public partial class UserControl1 : UserControl {
        [DllImport( "kernel32" )] private static extern bool AllocConsole();

        [DllImport( "kernel32.dll", SetLastError = true )]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport( "kernel32.dll" )] static extern IntPtr GetConsoleWindow();

        [DllImport( "kernel32.dll", SetLastError = true, ExactSpelling = true )]
        static extern bool FreeConsole();

        [DllImport( "user32.dll" )] public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


        [DllImport( "USER32.DLL" )] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport( "user32.dll" )] static extern bool DrawMenuBar(IntPtr hWnd);

        [DllImport( "user32.dll", EntryPoint = "SetWindowPos" )]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);


        [DllImport( "user32.dll", SetLastError = true )]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport( "user32", ExactSpelling = true, SetLastError = true )]
        private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref RECT rect, [MarshalAs( UnmanagedType.U4 )]
            int cPoints);

        [DllImport( "user32.dll", CharSet = CharSet.Auto, ExactSpelling = true )]
        private static extern IntPtr GetDesktopWindow();

        [StructLayout( LayoutKind.Sequential )]
        private struct RECT {
            public int left, top, bottom, right;
        }

        private const           int    GWL_STYLE      = -16;         //hex constant for style changing
        private const           int    WS_BORDER      = 0x00800000;  //window with border
        private const           int    WS_CAPTION     = 0x00C00000;  //window with a title bar
        private const           int    WS_SYSMENU     = 0x00080000;  //window with no borders etc.
        private const           int    WS_MINIMIZEBOX = 0x00020000;  //window with minimizebox
        private                 IntPtr hWndOriginalParent;

        private IntPtr hWndDocked;

        void MakeBorderless() {
            RECT rect;
            GetWindowRect( this.hWndDocked, out rect );
            IntPtr HWND_DESKTOP = GetDesktopWindow();
            MapWindowPoints( HWND_DESKTOP, this.hWndDocked, ref rect, 2 );
            SetWindowLong( this.hWndDocked, GWL_STYLE, WS_SYSMENU );
            SetWindowPos( this.hWndDocked, -2, 100, 75, rect.bottom, rect.right, 0x0040 );
            DrawMenuBar( this.hWndDocked );
        }

        public UserControl1() {
            InitializeComponent();
            this.hWndDocked = GetConsoleWindow();
        }

        private void undockIt() { SetParent( this.hWndDocked, this.hWndOriginalParent ); }


        void SetParrent() {
            this.hWndOriginalParent =  SetParent( this.hWndDocked, this.Handle );
            this.SizeChanged        += (sender, args) => MoveWindow( this.hWndDocked, 0, 0, this.Width, this.Height, true );
            this.Size               =  this.ClientSize;
            //DrawMenuBar( this.hWndDocked );       
            MoveWindow( this.hWndDocked, 0, 0, this.Width, this.Height, true );
        }

        private void UserControl1_Load(object sender, EventArgs e)
        {
            MakeBorderless();
            SetParrent();
        }
    }
}
