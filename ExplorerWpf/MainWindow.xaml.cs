//#define PerformanceTest

#region using

// ReSharper disable once RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExplorerWpf.Handler;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.Forms.MessageBox;

#endregion


namespace ExplorerWpf {
    public class ImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            try {
                if ( value is Bitmap bitmap1 ) {
                    var stream = new MemoryStream();
                    bitmap1.Save( stream, ImageFormat.Png );

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    return bitmap;
                }
            } catch (Exception e) {
                Debug.WriteLine( e );
                //SettingsHandler.OnError( e );
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public partial class MainWindow {
        private ExplorerView _currentExplorerView;


        private bool   _first;
        private Thread _inWriteThread;

        private Process _mainProcess;
        private Thread  _outReaderThread;
        private Thread  _errReaderThread;

        private TreePathItem _root;

        private bool SkipOneWrite;

        public MainWindow() {
            InitializeComponent();
            
            this.CopyRightTextBox.Text = Program.Version + "  " + Program.CopyRight;
        }

        private IHandler Handler => this._currentExplorerView.Handler;

        private void StartConsole() {
            var p = new Process { StartInfo = new ProcessStartInfo( "cmd.exe" ) };
            p.StartInfo.RedirectStandardInput  = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError  = true;
            p.StartInfo.CreateNoWindow         = true;
            p.StartInfo.UseShellExecute        = false;
            p.Start();
            p.StandardInput.AutoFlush = true;

            //p.BeginErrorReadLine();
            //p.BeginOutputReadLine();
            this._errReaderThread = new Thread( () => {
                while ( !p.StandardError.EndOfStream ) {
                    var line = p.StandardError.ReadLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine( line );
                    Console.ResetColor();
                }
            } );
            this._outReaderThread = new Thread( () => {
                p.StandardInput.WriteLine( "@echo off" );
                p.StandardInput.WriteLine( "cd /" );
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine( "Console Support Online" );
                Console.ResetColor();

                while ( !p.StandardOutput.EndOfStream ) {
                    var line = p.StandardOutput.ReadLine();

                    try {
                        var parches = Regex.Match( line, "[^\"]?[A-Za-z]:\\\\[^\"]*" );

                        if ( parches.Success ) {
                            var path = parches.Value.Substring( 0, parches.Length );

                            if ( Directory.Exists( path ) )
                                if ( this.Handler.GetCurrentPath() != path ) {
                                    //Console.ForegroundColor = ConsoleColor.DarkGreen;
                                    //Console.WriteLine( path );
                                    //Console.ResetColor();
                                    this.Handler.SetCurrentPath( path );
                                    this._currentExplorerView.ListP( this.Handler.GetCurrentPath(), true );
                                }
                        }
                    } catch { }

                    if ( !this.SkipOneWrite ) {
                        if ( line != "echo %cd%" )
                            Console.WriteLine( line );
                    }
                    else {
                        this.SkipOneWrite = false;
                    }
                }
            } );
            this._inWriteThread = new Thread( () => {
                while ( true ) {
                    var line = Console.In.ReadLine();
                    this.SkipOneWrite = true;
                    WriteCmd( line );
                }
            } );
            this._errReaderThread.Start();
            this._outReaderThread.Start();
            this._inWriteThread.Start();

            this.ConsoleW.Init();
            this._mainProcess          =  p;
            this.consoleHost.MouseDown += this.ConsoleW.MouseDownFocusWindow;
        }

        private void HandlerOnOnError(Exception obj) { SettingsHandler.OnError( obj ); }
                                                                                            
        private void MoveWindow(object sender, MouseButtonEventArgs e) {
            if ( e.LeftButton != MouseButtonState.Pressed ) return;

            if ( this.WindowState == WindowState.Maximized ) this.WindowState = WindowState.Normal;

            try {
                DragMove();
            } catch (Exception exception) {
                HandlerOnOnError( exception );
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            EnableBlur();

                StartConsole();


            this._root      = TreePathItem.Empty;
            this._root.Icon = DefaultIcons.ShieldIcon;
            this._root.Name = SettingsHandler.ROOT_FOLDER;
            this._root.Path = SettingsHandler.ROOT_FOLDER;
            this.TreeControl.Items.Add( this._root );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.System ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.Desktop ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.MyMusic ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.MyPictures ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.MyVideos ) ) ) );

            foreach ( var driveInfo in DriveInfo.GetDrives() ) {
                var node = new TreePathItem( driveInfo.RootDirectory );
                this.TreeControl.Items.Add( node );
            }

        #if PerformanceTest
            CreateContextMenu();
            var t = new Thread( () => {
                const int MaxTaps = 100;

                while ( true ) {
                    Thread.Sleep( 2000 );

                    for ( int i = 0; i < MaxTaps; i++ ) {
                        this.Dispatcher?.Invoke( () => { AddTabToTabControl( CreateExplorerTab( CreateExplorer( Path.GetDirectoryName( System.Windows.Forms.Application.ExecutablePath ) ) ) ); } );

                        Thread.Sleep( 50 );
                    }

                    Thread.Sleep( 1000 );

                    //for ( int i = 0; i < MaxTaps; i++ ) {
                    //    var fg = i;
                    //    this.Dispatcher?.Invoke( () => { this.TabControl.SelectedIndex = fg; } );
                    //    Thread.Sleep( 5 );
                    //}
                    //
                    //Thread.Sleep( 1000 );

                    for ( int i = 0; i < MaxTaps; i++ ) {
                        this.Dispatcher?.Invoke( () => { CloseTap( (TabItem) this.TabControl.Items[0] ); } );
                        Thread.Sleep( 50 );
                    }

                    Thread.Sleep( 1000 );
                    this.currentExplorerView = null;

                    GC.Collect( 1, GCCollectionMode.Forced, true );
                }
            } );

            t.SetApartmentState( ApartmentState.STA );
            t.Start();
        #endif

            AddTab();
        }

        private void DcChange(object sender, DependencyPropertyChangedEventArgs e) { Console.WriteLine( e ); }

        private void taps_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if ( this.TabControl.Items.Count == 1 ) {
                return;
            }

            if ( !( this.TabControl.SelectedItem is TabItem tp ) ) return;

            if ( tp.Content != null ) {
                if ( this._currentExplorerView != null && this._currentExplorerView.Equals( tp.Content ) ) return;

                if ( !( tp.Content is ExplorerView explorer ) ) return;

                this._currentExplorerView = explorer;
                var p = this.Handler.GetCurrentPath();

                if ( Regex.IsMatch( p, @"[A-Za-z]:\\" ) )
                    if ( SettingsHandler.ConsoleAutoChangeDisc )
                        WriteCmd( p.Substring( 0, 2 ) );

                if ( p.Length > 3 )
                    if ( SettingsHandler.ConsoleAutoChangePath )
                        WriteCmd( "cd \"" + p + "\"" );
            }
            else {
                if ( this.TabControl.Items.Count > 1 ) this.TabControl.SelectedIndex = this.TabControl.Items.Count - 2;
            }
        }

        private void WriteCmd(string command, bool echo = true) {
                if ( this._mainProcess == null || this._mainProcess.HasExited ) return;

                this._mainProcess.StandardInput.WriteLine( command );
                if ( echo && SettingsHandler.ConsoleAutoChangePath )
                    this._mainProcess.StandardInput.WriteLine( "echo %cd%" );

        }

        private void XOnSendDirectoryUpdateAsCmd(object sender, string e, bool cd) {
            if ( sender.Equals( this._currentExplorerView ) )
                WriteCmd( e, cd );
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e) {
            var me = (MenuItem) sender;

            switch (( (MenuItem) sender ).TabIndex) {
                case 1:
                    AddTab();
                    break;

                case 2:
                    if ( me.IsChecked )
                        MessageBox.Show( "Test" );
                    break;

                case 100:
                    if ( ( (ContextMenu) me.Parent ).PlacementTarget is Control parent )
                        if ( parent.Parent is TabItem tp && tp.Content != null )
                            CloseTap( tp );

                    break;
            }
        }

        private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { AddTab(); }

        // ReSharper disable once UnusedMember.Local
        private void DragMoveX(object sender, MouseButtonEventArgs e) { MoveWindow( sender, e ); }

        private void MainWindow_OnClosed(object sender, EventArgs e) {
            WriteCmd( "exit" );
            this._mainProcess?.Close();
            this._outReaderThread?.Abort();
            this._inWriteThread?.Abort();
            Environment.Exit( 0 );
        }

        #region Buttons

        private void CloseClick(object sender, RoutedEventArgs e) { Close(); }

        private void MaxClick(object sender, RoutedEventArgs e) { this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal; }

        private void MinClick(object sender, RoutedEventArgs e) {
            //if ( WindowState == WindowState.Normal ) 
            this.WindowState = WindowState.Minimized;
        }

        private void PingClick(object sender, RoutedEventArgs e) { this.Topmost = !this.Topmost; }

        private void UpClick(object sender, RoutedEventArgs e) {
            var dir = this._currentExplorerView.DirUp( this.Handler.GetCurrentPath() );
            this.Handler.SetCurrentPath( dir );
            this._currentExplorerView.ListP( dir );
        }

        private void RootClick(object sender, RoutedEventArgs e) {
            this.Handler.SetCurrentPath( SettingsHandler.ROOT_FOLDER );
            this._currentExplorerView.ListP( this.Handler.GetCurrentPath() );
        }

        #endregion

        #region TreeControal

        private void trvMenu_MouseDown(object sender, MouseButtonEventArgs e) { }

        private void trvMenu_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if ( this.TreeControl.SelectedItem == null ) return;

            try {
                this.Handler.SetCurrentPath( ( (TreePathItem) this.TreeControl.SelectedItem ).Path );
                this._currentExplorerView.ListP( this.Handler.GetCurrentPath() );
            } catch {
                // ignored
            }
        }

        private void trvMenu_Expanded(object sender, RoutedEventArgs e) {
            if ( !( e.OriginalSource is TreeViewItem tvi ) ) return;
            if ( !( tvi.DataContext is TreePathItem node ) ) return;
            if ( node.Type == Item.FileType.NONE ) return;

            try {
                node.Items.Clear();
                this.Handler.SetCurrentPath( node.Path + "\\" );

                foreach ( var t in this.Handler.ListDirectory( node.Path ) ) {
                    TreePathItem n1;

                    try {
                        n1 = new TreePathItem( t );

                        try {
                            if ( this.Handler.ListDirectory( this.Handler.GetCurrentPath() ).Length == 0 )
                                n1.Items.Clear(); //clear default item
                        } catch (Exception exception) {
                            var tcs = TreePathItem.Empty;
                            tcs.Name = exception.Message;

                            n1.Items.Add( tcs );
                        }
                    } catch {
                        n1      = TreePathItem.Empty;
                        n1.Name = t.Name;
                    }

                    node.Items.Add( n1 );
                }
            } catch {
                // ignored
            }
        }

        private void trvMenu_Collapsed(object sender, RoutedEventArgs e) { }

        #endregion

        #region Blur

        [DllImport( "user32.dll" )] private static extern int SetWindowCompositionAttribute(IntPtr hWnd, ref WindowCompositionAttributeData data);

        [StructLayout( LayoutKind.Sequential )]
        private struct WindowCompositionAttributeData {
            public WindowCompositionAttribute Attribute;
            public IntPtr                     Data;
            public int                        SizeOfData;
        }

        private enum WindowCompositionAttribute {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        private enum AccentState {
            // ReSharper disable UnusedMember.Local

            ACCENT_DISABLED                   = 0,
            ACCENT_ENABLE_GRADIENT            = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND          = 3,
            ACCENT_INVALID_STATE              = 4

            // ReSharper restore UnusedMember.Local
        }

        [StructLayout( LayoutKind.Sequential )]
        private struct AccentPolicy {
            public           AccentState AccentState;
            private readonly int         AccentFlags;
            private readonly int         GradientColor;
            private readonly int         AnimationId;
        }

        private void EnableBlur() {
            var windowHelper = new WindowInteropHelper( this );

            var accent           = new AccentPolicy();
            var accentStructSize = Marshal.SizeOf( accent );
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentPtr = Marshal.AllocHGlobal( accentStructSize );
            Marshal.StructureToPtr( accent, accentPtr, false );

            var data = new WindowCompositionAttributeData { Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY, SizeOfData = accentStructSize, Data = accentPtr };

            SetWindowCompositionAttribute( windowHelper.Handle, ref data );

            Marshal.FreeHGlobal( accentPtr );
        }

        #endregion

        #region TabS

        private ContextMenu _contextMenu;

        private void CreateContextMenu() {
            if ( this._contextMenu == null ) {
                var m = new ContextMenu();

                var me1 = new MenuItem { TabIndex = 100, Header = "_Close" };
                me1.Click += MenuItem_OnClick;

                m.Items.Add( me1 );
                this._contextMenu = m;
            }
        }

        private ExplorerView CreateExplorer(string path = SettingsHandler.ROOT_FOLDER) {
            var h = new LocalHandler( path );
            h.OnError += HandlerOnOnError;

            var x = new ExplorerView( new WindowInteropHelper( this ).Handle );
        #if PerformanceTest
            var t = new Thread( () => {
        #endif
            x.Init( h );

            x.SendDirectoryUpdateAsCmd += XOnSendDirectoryUpdateAsCmd;
            x.UpdateStatusBar          += XOnUpdateStatusBar;
        #if PerformanceTest
            } );
            t.Start();
        #endif
            x.Margin = new Thickness( 0, 0, 0, 0 );
            return x;
        }

        private void XOnUpdateStatusBar(object arg1, string arg2, Brush arg3) {
            this.StatusBar.Foreground = arg3;
            this.StatusBar.Text       = arg2;
        }

        private TabItem CreateExplorerTab(ExplorerView explorerView) {
            var ex = (Label) this.PlusTabItem.Header;
            var l  = new Label { Content = "Explorer", Style = ex.Style, Margin = ex.Margin, FontSize = ex.FontSize, Effect = ex.Effect, Foreground = ex.Foreground, Background = ex.Background, BorderBrush = ex.BorderBrush, BorderThickness = ex.BorderThickness, ContextMenu = this._contextMenu };

            var newTabItem = new TabItem {
                Header  = l,
                Name    = "explorer",
                Content = explorerView,
                //Background  = this.ColorExample.Background,
                //Foreground  = this.ColorExample.Foreground,
                //BorderBrush = this.ColorExample.BorderBrush,
                Style = this.PlusTabItem.Style
            };
            explorerView.setTapItem( newTabItem );
            return newTabItem;
        }

        private void AddTabToTabControl(TabItem newTabItem) {
            this.TabControl.Items.Add( new TabItem { Header = "Temp" } );
            var p = this.TabControl.Items[this.TabControl.Items.Count - 2];
            this.TabControl.Items[this.TabControl.Items.Count - 2] = newTabItem;
            this.TabControl.Items[this.TabControl.Items.Count - 1] = p;

            this.TabControl.SelectedIndex = this.TabControl.Items.Count - 2;
            taps_SelectionChanged( this, null );
        }

        private void AddTab(string path = SettingsHandler.ROOT_FOLDER) {
            if ( this._contextMenu == null ) CreateContextMenu();

            AddTabToTabControl( CreateExplorerTab( CreateExplorer( path ) ) );
        }

        private void CloseTap(TabItem tp) {
            if ( !( tp.Content is ExplorerView explorer ) ) return;

            explorer.Dispose();
            tp.Content = null;
            this.TabControl.Items.Remove( tp );
            GC.Collect( 0, GCCollectionMode.Forced );
            if ( this.TabControl.Items.Count == 1 ) this.CloseClick( null, null );
        }

        #endregion

    }
}
