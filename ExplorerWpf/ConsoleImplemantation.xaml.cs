using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserControl = System.Windows.Controls.UserControl;

namespace ExplorerWpf {
    /// <summary>
    /// Interaktionslogik für ConsoleImplemantation.xaml
    /// </summary>
    public partial class ConsoleImplemantation : UserControl {
        [DllImport( "user32.dll" )]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport( "kernel32" )] private static extern bool AllocConsole();

        [DllImport( "kernel32.dll", SetLastError = true )]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport( "kernel32.dll" )] static extern IntPtr GetConsoleWindow();

        [DllImport( "kernel32.dll", SetLastError = true, ExactSpelling = true )]
        static extern bool FreeConsole();

        [DllImport( "user32.dll", EntryPoint = "SetLayeredWindowAttributes" )]
        static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

        [DllImport( "user32.dll" )] public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


        [DllImport( "USER32.DLL" )] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport( "user32.dll" )] static extern bool DrawMenuBar(IntPtr hWnd);

        [DllImport( "user32.dll", EntryPoint = "SetWindowPos" )]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport( "user32.dll" )] private static extern
            bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport( "user32.dll", SetLastError = true )]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport( "user32", ExactSpelling = true, SetLastError = true )]
        private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref RECT rect, [MarshalAs( UnmanagedType.U4 )]
            int cPoints);

        [DllImport( "user32.dll", CharSet = CharSet.Auto, ExactSpelling = true )]
        private static extern IntPtr GetDesktopWindow();

        [StructLayout( LayoutKind.Sequential )]
        private struct RECT {
            public int left, top, bottom, right;
        }

        private const int    GWL_STYLE      = -16;        //hex constant for style changing
        private const int    WS_BORDER      = 0x00800000; //window with border
        private const int    WS_CAPTION     = 0x00C00000; //window with a title bar
        private const int    WS_SYSMENU     = 0x00080000; //window with no borders etc.
        private const int    WS_MINIMIZEBOX = 0x00020000; //window with minimizebox
        private const int    SW_HIDE        = 0;
        private const int    SW_SHOWNORMAL  = 1;
        private const int    SW_RESTORE     = 9;
        private       IntPtr hWndOriginalParent;

        private IntPtr hWndDocked;
        private IntPtr hWndOParent;

        void MakeBorderless() {
            RECT rect;
            GetWindowRect( this.hWndDocked, out rect );
            IntPtr HWND_DESKTOP = GetDesktopWindow();
            MapWindowPoints( HWND_DESKTOP, this.hWndDocked, ref rect, 2 );
            SetWindowLong( this.hWndDocked, GWL_STYLE, WS_SYSMENU );
            SetWindowPos( this.hWndDocked, -2, 100, 75, rect.bottom, rect.right, 0x0040 );
            DrawMenuBar( this.hWndDocked );
        }

        private Panel rot;

        public ConsoleImplemantation() {
            InitializeComponent();

            this.rot                       =  new Panel();
            this.rot.Dock                  =  DockStyle.Fill;
            this.host.Child                =  this.rot;
            this.hWndOParent               =  this.rot.Handle;
            this.host.MouseDown            += MouseDownFocusWindow;
            this.ParterreControl.MouseDown += MouseDownFocusWindow;
            this.MouseDown                 += MouseDownFocusWindow;
        }

        public void MouseDownFocusWindow(object sender, MouseButtonEventArgs e) { SetForegroundWindow( this.hWndDocked ); }

        private void undockIt() { SetParent( this.hWndDocked, this.hWndOriginalParent ); }


        void SetParrent() {
            this.hWndOriginalParent =  SetParent( this.hWndDocked, this.hWndOParent );
            this.SizeChanged        += ( (sender, args) => MoveWindow( this.hWndDocked, 0, 0, this.rot.Width, this.rot.Height, true ) );

            MoveWindow( this.hWndDocked, 0, 0, this.rot.Width, this.rot.Height, true );
            SetWindowLong( this.hWndDocked, -20, 524288  ); //GWL_EXSTYLE=-20; WS_EX_LAYERED=524288=&h80000, WS_EX_TRANSPARENT=32=0x00000020L
            SetLayeredWindowAttributes( hWndDocked, 0, 75, 2 ); // Transparency=51=20%, LWA_ALPHA=2
        }

        public void Init() { Init( GetConsoleWindow() ); }

        public void Init(IntPtr hWnd) {
            this.hWndDocked = hWnd;
            this.Dispatcher.Invoke( () => MakeBorderless() );
            this.Dispatcher.Invoke( () => SetParrent() );
        }

        public void Init(IntPtr hWnd, bool noparent) {
            this.hWndDocked = hWnd;
            this.Dispatcher.Invoke( MakeBorderless );
            this.Dispatcher.Invoke( () => {
                SetWindowLong( this.hWndDocked, -20, ( 524288 ) );
                SetLayeredWindowAttributes( hWndDocked, 0, 95, 2 );
            } );
        }

        public void HideConsole() { ShowWindowAsync( this.hWndDocked, SW_HIDE ); }

        public void ShowConsole() { ShowWindowAsync( this.hWndDocked, SW_SHOWNORMAL ); }

        public void MoveConsole() {
            this.Dispatcher.Invoke( () => {
                try {
                    var p = this.PointToScreen( new Point( 0, 0 ) );
                    var h = new Point( this.RenderSize.Width, this.RenderSize.Height );
                    Debug.WriteLine( p + " " + h );
                    MoveWindow( this.hWndDocked, (int) p.X, (int) p.Y, (int) h.X, (int) h.Y, true );
                } catch { }
            } );
        }

        private void ConsoleImplemantation_OnLoaded(object sender, RoutedEventArgs e) { new Thread( () => { Thread.Sleep( 1000 ); } ).Start(); }
    }
}
