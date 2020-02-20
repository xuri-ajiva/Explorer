//#define PerformanceTest

#region using

// ReSharper disable once RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ConsoleControlAPI;
using ExplorerWpf.Handler;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.Forms.MessageBox;

#endregion


namespace ExplorerWpf {
    public class ImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if ( value is Bitmap bitmap1 ) {
                var stream = new MemoryStream();
                bitmap1.Save( stream, ImageFormat.Png );

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.EndInit();

                return bitmap;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
          

    /// <summary>
    ///     Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow {


        private ExplorerView _currentExplorerView;


        private bool _first;

        /*const int GripSize   = 16;
        const int BorderSize = 7;

        ConsoleContent dc = new ConsoleContent();*/

        public MainWindow() {
            //AllocConsole();
            InitializeComponent(); 
            this.consoleX.InitializeComponent();
            this.consoleX.OnProcessOutput += ConsoleXOnOnProcessOutput;
            this.consoleX.OnProcessInput  += ConsoleXOnOnProcessInput;
            this.consoleX.StartProcess( "cmd.exe", "" );
            this.consoleX.IsInputEnabled = true;
            this.consoleX.Visibility     = Visibility.Collapsed;

            this.coppyRightTextBox.Text =Program.Version + "  "+ Program.CopyRight;
        }

        private IHandler Handler => this._currentExplorerView.Handler;

        // ReSharper disable once UnusedMember.Local
        [DllImport( "kernel32" )] private static extern bool AllocConsole();

        private void ConsoleXOnOnProcessInput(object sender, ProcessEventArgs args) { }


        private void ConsoleXOnOnProcessOutput(object sender, ProcessEventArgs args) {
            if ( !this._first ) {

                this.consoleX.WriteOutput( "\nConsole Support Enabled!\n", Color.FromRgb( 0, 129, 255 ) );
                this.consoleX.Visibility = Visibility.Visible;
                this._first              = true;
            }

            if ( args.Code.HasValue ) this.consoleX.WriteOutput( $"[{args.Code.Value}]", Colors.DarkBlue );

            //if ( Regex.IsMatch( args.Content, @"[A-Z]:\\[^>]*>" ) ) {
            this.consoleX.WriteOutput( "> ", Colors.Yellow );
            this.consoleX.WriteOutput( " ",  Colors.DeepSkyBlue );
            //}
        }

        private void CloseClick(object sender, RoutedEventArgs e) { Close(); }

        private void MaxClick(object sender, RoutedEventArgs e) { this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal; }

        private void MinClick(object sender, RoutedEventArgs e) {
            //if ( WindowState == WindowState.Normal ) 
            this.WindowState = WindowState.Minimized;
        }

        private void PingClick(object sender, RoutedEventArgs e) { this.Topmost = !this.Topmost; }

        private void UpClick(object sender, RoutedEventArgs e) { }

        private void RootClick(object sender, RoutedEventArgs e) {
            this.Handler.SetCurrentPath( LocalHandler.ROOT_FOLDER );
            this._currentExplorerView.List( this.Handler.GetCurrentPath() );
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e) {
            if ( e.LeftButton != MouseButtonState.Pressed ) return;

            if ( this.WindowState == WindowState.Maximized ) this.WindowState = WindowState.Normal;

            try {
                DragMove();
            } catch (Exception exception) {
                Console.WriteLine( exception );
            }
        }

        private void trvMenu_MouseDown(object sender, MouseButtonEventArgs e) { }

        private void trvMenu_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if ( this.TreeControl.SelectedItem == null ) return;

            try {
                this.Handler.SetCurrentPath( ( (TreePathItem) this.TreeControl.SelectedItem ).Path + "\\" );
                this._currentExplorerView.List( this.Handler.GetCurrentPath() );
            } catch {
                // ignored
            }
        }

        private void trvMenu_Expanded(object sender, RoutedEventArgs e) {
            if ( !( e.OriginalSource is TreeViewItem tvi ) ) return;

            //var node = tvi;

            //MessageBox.Show( string.Format( "TreeNode '{0}' was expanded", tvi.Header ) );
            if ( !( tvi.DataContext is TreePathItem node ) ) return;

            try {
                this.Handler.SetCurrentPath( node.Path + "\\" );

                foreach ( var t in this.Handler.ListDirectory( node.Path ) ) {
                    TreePathItem n1;

                    try {
                        n1 = new TreePathItem( t );

                        try {
                            if ( this.Handler.ListDirectory( this.Handler.GetCurrentPath() ).Length > 0 )
                                n1.Items.Add( TreePathItem.Empty );
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


        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            EnableBlur();

            foreach ( var driveInfo in DriveInfo.GetDrives() ) {
                var node = new TreePathItem( driveInfo.RootDirectory );
                node.Items.Add( TreePathItem.Empty );
                this.TreeControl.Items.Add( node );
            }

            //
            //
            // TODO: NetworkHandler
            //
            // 
            //var i = 0;
            //
            //foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":" ).Where( this._handler.DirectoryExists ) ) {
            //    TreePathItem node = new TreePathItem() { Name = dir, PathAbs = dir };
            //    node.Items.Add( new TreePathItem { Name       = "empty" } );
            //    this.trvMenu.Items.Add( node );
            //    //var e = new TreeViewEventArgs( this.listBrowderView.Nodes[i] );
            //    //treeView1_AfterExpand( null, e );
            //    i++;
            //}
            //
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
            if ( !( this.TabControl.SelectedItem is TabItem tp ) ) return;

            if ( tp.Content != null ) {
                if ( this._currentExplorerView != null && this._currentExplorerView.Equals( tp.Content ) ) return;

                if ( !( tp.Content is ExplorerView explorer ) /*TODO: Check if nesselrode (|| !explorer.InitDone)*/ ) return;

                this._currentExplorerView = explorer;
                var p = this.Handler.GetCurrentPath();

                if ( Regex.IsMatch( p, @"[A-Za-z]:\\" ) ) this.consoleX.ProcessInterface.WriteInput( p.Substring( 0, 2 ) );

                if ( p.Length > 3 )
                    this.consoleX.ProcessInterface.WriteInput( "cd \"" + p + "\"" );
            }
            else {
                if ( this.TabControl.Items.Count > 1 ) this.TabControl.SelectedIndex = this.TabControl.Items.Count - 2;
            }
        }

        private void XOnSendDirectoryUpdateAsCmd(object sender, string e) {
            if ( sender.Equals( this._currentExplorerView ) ) this.consoleX.ProcessInterface.WriteInput( e );
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
        private void DragmoveX(object sender, MouseButtonEventArgs e) { MoveWindow( sender, e ); }

        #region Blur

        [DllImport( "user32.dll" )]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout( LayoutKind.Sequential )]
        internal struct WindowCompositionAttributeData {
            public WindowCompositionAttribute Attribute;
            public IntPtr                     Data;
            public int                        SizeOfData;
        }

        internal enum WindowCompositionAttribute {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        internal enum AccentState {
            ACCENT_DISABLED                   = 0,
            ACCENT_ENABLE_GRADIENT            = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND          = 3,
            ACCENT_INVALID_STATE              = 4
        }

        [StructLayout( LayoutKind.Sequential )]
        internal struct AccentPolicy {
            public AccentState AccentState;
            public int         AccentFlags;
            public int         GradientColor;
            public int         AnimationId;
        }

        internal void EnableBlur() {
            var windowHelper = new WindowInteropHelper( this );

            var accent           = new AccentPolicy();
            var accentStructSize = Marshal.SizeOf( accent );
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentPtr = Marshal.AllocHGlobal( accentStructSize );
            Marshal.StructureToPtr( accent, accentPtr, false );

            var data = new WindowCompositionAttributeData();
            data.Attribute  = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data       = accentPtr;

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

        private ExplorerView CreateExplorer(string path = LocalHandler.ROOT_FOLDER) {
            var h = new LocalHandler( path );
            var x = new ExplorerView( new WindowInteropHelper( this ).Handle, this.consoleX );
        #if PerformanceTest
            var t = new Thread( () => {
        #endif
            x.Init( h );

            x.SendDirectoryUpdateAsCmd += XOnSendDirectoryUpdateAsCmd;   
            x.UpdateStatusBar += XOnUpdateStatusBar;
        #if PerformanceTest
            } );
            t.Start();
        #endif
            x.Margin = new Thickness( 0, 0, 0, 0 );
            return x;
        }

        private void XOnUpdateStatusBar(object arg1, string arg2, Brush arg3) {
            StatusBar.Foreground = arg3;
            StatusBar.Text = arg2;

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

        private void AddTab(string path = LocalHandler.ROOT_FOLDER) {
            if ( this._contextMenu == null ) CreateContextMenu();

            AddTabToTabControl( CreateExplorerTab( CreateExplorer( path ) ) );
        }

        private void CloseTap(TabItem tp) {
            if ( !( tp.Content is ExplorerView explorer ) ) return;

            explorer.Dispose();
            tp.Content = null;
            this.TabControl.Items.Remove( tp );
            GC.Collect( 0, GCCollectionMode.Forced );
        }

        #endregion

    }
}
