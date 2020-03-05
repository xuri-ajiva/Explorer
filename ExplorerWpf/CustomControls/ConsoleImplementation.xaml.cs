#region using

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Panel = System.Windows.Forms.Panel;
using UserControl = System.Windows.Controls.UserControl;

#endregion

namespace ExplorerWpf.CustomControls {
    /// <summary>
    ///     Interaktionslogik für ConsoleImplementation.xaml
    /// </summary>
    public partial class ConsoleImplementation : UserControl, IDisposable {
        private readonly IntPtr _hWndOParent;

        private readonly Panel _rot;

        private IntPtr _hWndDocked;

        private IntPtr _hWndOriginalParent;

        public ConsoleImplementation() {
            InitializeComponent();

            this._rot                      =  new Panel();
            this._rot.Dock                 =  DockStyle.Fill;
            this.Host.Child                =  this._rot;
            this._hWndOParent              =  this._rot.Handle;
            this.Host.MouseDown            += MouseDownFocusWindow;
            this.ParterreControl.MouseDown += MouseDownFocusWindow;
            this.MouseDown                 += MouseDownFocusWindow;
        }

        private void MakeBorderless() {
            NativeMethods.RECT rect;
            NativeMethods.GetWindowRect( this._hWndDocked, out rect );
            var hwndDesktop = NativeMethods.GetDesktopWindow();

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            NativeMethods.MapWindowPoints( hwndDesktop, this._hWndDocked, ref rect, 2 );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            NativeMethods.SetWindowLong( this._hWndDocked, NativeMethods.GWL_STYLE, NativeMethods.WS_CAPTION );
            NativeMethods.SetWindowPos( this._hWndDocked, -2, 200, 150, rect.bottom, rect.right, 0x0040 );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 800 );
            NativeMethods.SetWindowLong( this._hWndDocked, NativeMethods.GWL_STYLE, NativeMethods.WS_SYSMENU );
            NativeMethods.SetWindowPos( this._hWndDocked, -2, 100, 75, rect.bottom, rect.right, 0x0040 );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            NativeMethods.DrawMenuBar( this._hWndDocked );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
        }

        public void MouseDownFocusWindow(object sender, MouseButtonEventArgs e) { NativeMethods.SetForegroundWindow( this._hWndDocked ); }

        private void UndockIt() { NativeMethods.SetParent( this._hWndDocked, this._hWndOriginalParent ); }


        private void SetParrent() {
            this._hWndOriginalParent = NativeMethods.SetParent( this._hWndDocked, this._hWndOParent );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            this.SizeChanged += (sender, args) => NativeMethods.MoveWindow( this._hWndDocked, 0, 0, this._rot.Width, this._rot.Height, true );

            NativeMethods.MoveWindow( this._hWndDocked, 0, 0, this._rot.Width, this._rot.Height, true );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            NativeMethods.SetWindowLong( this._hWndDocked, -20, 524288 ); //GWL_EXSTYLE=-20; WS_EX_LAYERED=524288=&h80000, WS_EX_TRANSPARENT=32=0x00000020L

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );

            NativeMethods.SetLayeredWindowAttributes( this._hWndDocked, 0, 75, 2 ); // Transparency=51=20%, LWA_ALPHA=2

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
        }

        public void Init() {
            NativeMethods.AllocConsole();
            Init( NativeMethods.GetConsoleWindow() );
        }

        private void Init(IntPtr hWnd) {
            this._hWndDocked = hWnd;
            this.Dispatcher.Invoke( MakeBorderless );
            this.Dispatcher.Invoke( SetParrent );
        }

        public void Init(IntPtr hWnd, bool noparent) {
            this._hWndDocked = hWnd;
            this.Dispatcher.Invoke( MakeBorderless );
            this.Dispatcher.Invoke( () => {
                NativeMethods.SetWindowLong( this._hWndDocked, -20, 524288 );
                NativeMethods.SetLayeredWindowAttributes( this._hWndDocked, 0, 95, 2 );
            } );
        }

        ~ConsoleImplementation() {
            UndockIt();
            Dispose( false );
        }

        public void HideConsole() { NativeMethods.ShowWindowAsync( this._hWndDocked, NativeMethods.SW_HIDE ); }

        public void ShowConsole() { NativeMethods.ShowWindowAsync( this._hWndDocked, NativeMethods.SW_SHOWNORMAL ); }

        public void MoveConsole() {
            this.Dispatcher.Invoke( () => {
                try {
                    var p = PointToScreen( new Point( 0, 0 ) );
                    var h = new Point( this.RenderSize.Width, this.RenderSize.Height );
                    Debug.WriteLine( p + " " + h );
                    NativeMethods.MoveWindow( this._hWndDocked, (int) p.X, (int) p.Y, (int) h.X, (int) h.Y, true );
                } catch { }
            } );
        }

        private void ConsoleImplemantation_OnLoaded(object sender, RoutedEventArgs e) { new Thread( () => { Thread.Sleep( 1000 ); } ).Start(); }

        #region IDisposable

        private void ReleaseUnmanagedResources() {
            // TODO release unmanaged resources here
        }

        private void Dispose(bool disposing) {
            ReleaseUnmanagedResources();

            if ( disposing ) {
                this.Host?.Dispose();
                this._rot?.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        #endregion

    }
}
