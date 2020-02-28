﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using static ExplorerWpf.SettingsHandler.NativeMethods;
using UserControl = System.Windows.Controls.UserControl;

namespace ExplorerWpf {
    /// <summary>
    /// Interaktionslogik für ConsoleImplementation.xaml
    /// </summary>
    public partial class ConsoleImplementation : UserControl, IDisposable {

        private IntPtr hWndOriginalParent;

        private IntPtr hWndDocked;
        private IntPtr hWndOParent;

        void MakeBorderless() {
            RECT rect;
            GetWindowRect( this.hWndDocked, out rect );
            IntPtr HWND_DESKTOP = GetDesktopWindow();

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            MapWindowPoints( HWND_DESKTOP, this.hWndDocked, ref rect, 2 );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            SetWindowLong( this.hWndDocked, GWL_STYLE, WS_CAPTION );
            SetWindowPos( this.hWndDocked, -2, 200, 150, rect.bottom, rect.right, 0x0040 );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 800 );
            SetWindowLong( this.hWndDocked, GWL_STYLE, WS_SYSMENU );
            SetWindowPos( this.hWndDocked, -2, 100, 75, rect.bottom, rect.right, 0x0040 );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            DrawMenuBar( this.hWndDocked );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
        }

        private Panel rot;

        public ConsoleImplementation() {
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
            this.hWndOriginalParent = SetParent( this.hWndDocked, this.hWndOParent );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            this.SizeChanged += ( (sender, args) => MoveWindow( this.hWndDocked, 0, 0, this.rot.Width, this.rot.Height, true ) );

            MoveWindow( this.hWndDocked, 0, 0, this.rot.Width, this.rot.Height, true );

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
            SetWindowLong( this.hWndDocked, -20, 524288 ); //GWL_EXSTYLE=-20; WS_EX_LAYERED=524288=&h80000, WS_EX_TRANSPARENT=32=0x00000020L

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );

            SetLayeredWindowAttributes( hWndDocked, 0, 75, 2 ); // Transparency=51=20%, LWA_ALPHA=2

            if ( SettingsHandler.ConsolePresent ) Thread.Sleep( 400 );
        }

        public void Init() {
            AllocConsole();
            Init( GetConsoleWindow() );
        }

        public void Init(IntPtr hWnd) {
            this.hWndDocked = hWnd;
            this.Dispatcher.Invoke( MakeBorderless );
            this.Dispatcher.Invoke( SetParrent );
        }

        public void Init(IntPtr hWnd, bool noparent) {
            this.hWndDocked = hWnd;
            this.Dispatcher.Invoke( MakeBorderless );
            this.Dispatcher.Invoke( () => {
                SetWindowLong( this.hWndDocked, -20, ( 524288 ) );
                SetLayeredWindowAttributes( hWndDocked, 0, 95, 2 );
            } );
        }

        ~ConsoleImplementation() { Dispose( false ); }

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

        #region IDisposable

        private void ReleaseUnmanagedResources() {
            // TODO release unmanaged resources here
        }

        private void Dispose(bool disposing) {
            ReleaseUnmanagedResources();

            if ( disposing ) {
                this.host?.Dispose();
                this.rot?.Dispose();
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