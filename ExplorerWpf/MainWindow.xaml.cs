//#define PerformanceTest

#region using

// ReSharper disable once RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ConsoleControlAPI;
using ExplorerBase.Handlers;
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
    public class Item {


        [Obsolete( "User File or Directory Info!", true )]
        public Item(string name, string path, string size, FileType type) {
            this.Name = name;
            this.Path = path.Replace( "\\\\", "\\" );
            this.Size = size;
            this.Type = type;

            CreateIcon();
        }

        public Item(FileInfo f) {
            this.Path           = f.FullName.Replace( "\\\\", "\\" );
            this.Name           = f.Name;
            this.Size           = f.Length + $" ({GetLenght( f.Length )})";
            this.Exists         = f.Exists;
            this.IsReadOnly     = f.IsReadOnly;
            this.Extension      = f.Extension;
            this.CreationTime   = f.CreationTime;
            this.LastAccessTime = f.LastAccessTime;
            this.LastWriteTime  = f.LastWriteTime;

            this.Type = FileType.FILE;

            CreateIcon();
        }

        public Item(DirectoryInfo d) {
            this.Path           = d.FullName.Replace( "\\\\", "\\" );
            this.Name           = d.Name;
            this.Size           = "0";
            this.Exists         = d.Exists;
            this.IsReadOnly     = false;
            this.Extension      = d.Extension;
            this.CreationTime   = d.CreationTime;
            this.LastAccessTime = d.LastAccessTime;
            this.LastWriteTime  = d.LastWriteTime;

            this.Type = FileType.DIRECTORY;

            CreateIcon();
        }


        protected Item() {
            this.Icon           = SystemIcons.Hand.ToBitmap();
            this.Name           = "empty";
            this.Path           = "empty";
            this.Size           = int.MaxValue.ToString();
            this.Type           = FileType.NONE;
            this.Exists         = false;
            this.IsReadOnly     = true;
            this.Extension      = "*";
            this.CreationTime   = DateTime.MaxValue;
            this.LastAccessTime = DateTime.MaxValue;
            this.LastWriteTime  = DateTime.MaxValue;
        }


        public static Item Empty => new Item();

        public static Item Root {
            get {
                var i = new Item { Path = "/", Name = "/", Type = FileType.DIRECTORY, Size = long.MaxValue + $" ({GetLenght( long.MaxValue )})" };
                i.Icon.Dispose();
                i.Icon = SystemIcons.Shield.ToBitmap();
                return i;
            }
        }

        [DebuggerStepThrough]
        public static string GetFileLenght(string fileName) => GetLenght( new FileInfo( fileName ).Length );

        [DebuggerStepThrough]
        public static string GetLenght(long length) {
            if ( length > Math.Pow( 10, 15 ) ) return ( length / Math.Pow( 10, 15 ) ).ToString( "0.00" ) + "Pb";
            if ( length > Math.Pow( 10, 12 ) ) return ( length / Math.Pow( 10, 12 ) ).ToString( "0.00" ) + "Tb";
            if ( length > Math.Pow( 10, 9 ) ) return ( length / Math.Pow( 10, 9 ) ).ToString( "0.00" )   + "Gb";
            if ( length > Math.Pow( 10, 6 ) ) return ( length / Math.Pow( 10, 6 ) ).ToString( "0.00" )   + "Mb";
            if ( length > Math.Pow( 10, 3 ) ) return ( length / Math.Pow( 10, 3 ) ).ToString( "0.00" )   + "Kb";

            return length + "b";
        }

        private void CreateIcon() {
            try {
                this.Icon = this.Path != "/" ? DefaultIcons.GetFileIconCashed( this.Path ).ToBitmap() : SystemIcons.Shield.ToBitmap();
            } catch (Exception e) {
                try {
                    this.Icon = SystemIcons.Error.ToBitmap();
                } catch (Exception exception) {
                    Console.WriteLine( exception );
                }

                Console.WriteLine( e.Message );
            }
        }

        ~Item() { this.Icon.Dispose(); }


        // ReSharper disable UnusedAutoPropertyAccessor.Global

        public Bitmap   Icon           { get; private set; }
        public string   Name           { get; set; }
        public string   Path           { get; private set; }
        public string   Size           { get; set; }
        public FileType Type           { get; private set; }
        public string   Extension      { get; }
        public bool     Exists         { get; }
        public bool     IsReadOnly     { get; }
        public DateTime CreationTime   { get; }
        public DateTime LastAccessTime { get; }
        public DateTime LastWriteTime  { get; }

        // ReSharper restore UnusedAutoPropertyAccessor.Global
    }
    public static class DefaultIcons {

        private const uint SHGFI_ICON      = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;

        // ReSharper disable once UnusedMember.Local
        private const           uint                     SHGFI_SMALLICON = 0x000000001;
        private static readonly Dictionary<string, Icon> Cache           = new Dictionary<string, Icon>();

        public static Icon GetFileIconCashed(string path) {
            var ext = path;

            if ( File.Exists( path ) ) ext = Path.GetExtension( path );

            if ( ext == null )
                return null;

            Icon icon;
            if ( Cache.TryGetValue( ext, out icon ) )
                return icon;

            icon = ExtractFromPath( path );
            Cache.Add( ext, icon );
            return icon;
        }


        private static Icon ExtractFromPath(string path) {
            var shinfo = new SHFILEINFO();
            SHGetFileInfo( path, 0, ref shinfo, (uint) Marshal.SizeOf( shinfo ), SHGFI_ICON | SHGFI_LARGEICON );
            return Icon.FromHandle( shinfo.hIcon );
        }

        [DllImport( "shell32.dll" )]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        //Struct used by SHGetFileInfo function
        [StructLayout( LayoutKind.Sequential )]
        // ReSharper disable once InconsistentNaming
        private struct SHFILEINFO {
            public readonly IntPtr hIcon;
            public readonly int    iIcon;
            public readonly uint   dwAttributes;

            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
            public readonly string szDisplayName;

            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 80 )]
            public readonly string szTypeName;
        }
    }

    public enum FileType {
        DIRECTORY, FILE, NONE
    }

    public class TreePathItem : Item {

        public TreePathItem(DirectoryInfo d) : base( d ) { Init(); }
        public TreePathItem(FileInfo      f) : base( f ) { Init(); }

        // ReSharper disable once RedundantBaseConstructorCall
        protected TreePathItem() : base() { }

        public new static TreePathItem Empty => new TreePathItem();

        public  ObservableCollection<TreePathItem> Items  { get; set; }
        private void                               Init() { this.Items = new ObservableCollection<TreePathItem>(); }
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
            /*DataContext =  dc;
            Loaded      += MainWindow_Loaded;*/
            //this.consoleX.ShowDiagnostics = true;   
            this.consoleX.InitializeComponent();
            this.consoleX.OnProcessOutput += ConsoleXOnOnProcessOutput;
            this.consoleX.OnProcessInput  += ConsoleXOnOnProcessInput;
            this.consoleX.StartProcess( "cmd.exe", "" );
            this.consoleX.IsInputEnabled = true;
            this.consoleX.Visibility     = Visibility.Collapsed;
            this.consoleX.Foreground     = Brushes.LimeGreen;

            //this._handler = new LocalHandler( "C:\\" );

            //TreePathItem root       = new TreePathItem() { Name = "Menu" };
            //TreePathItem childItem1 = new TreePathItem() { Name = "Child item #1" };
            //childItem1.Items.Add(new TreePathItem() { Name = "Child item #1.1" });
            //childItem1.Items.Add(new TreePathItem() { Name = "Child item #1.2" });
            //root.Items.Add(childItem1);
            //root.Items.Add(new TreePathItem() { Name = "Child item #2" });
            //trvMenu.Items.Add(root);
        }

        private IHandler Handler => this._currentExplorerView.Handler;

        // ReSharper disable once UnusedMember.Local
        [DllImport( "kernel32" )] private static extern bool AllocConsole();

        private void ConsoleXOnOnProcessInput(object sender, ProcessEventArgs args) { }


        private void ConsoleXOnOnProcessOutput(object sender, ProcessEventArgs args) {
            if ( !this._first ) {
                //new Thread( () => {
                //    Thread.Sleep( 1000 );
                //
                //    var dispatcher = this.Dispatcher;
                //
                //    if ( dispatcher != null ) {
                //        dispatcher.Invoke( () => { this.consoleX.ProcessInterface.WriteInput( "@echo off" ); } );
                //        Thread.Sleep( 100 );
                //        dispatcher.Invoke( () => {
                //            this.consoleX.ClearOutput();
                //            this.consoleX.WriteOutput( "Console Support Enabled!\n", Color.FromRgb( 0, 129, 255 ) );
                //            this.consoleX.Visibility = Visibility.Visible;
                //        } );
                //    }
                //    else { }
                //} ); //.Start();
                this.consoleX.WriteOutput( "Console Support Enabled!\n", Color.FromRgb( 0, 129, 255 ) );
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

        private void MoveWindow(object sender, MouseButtonEventArgs e) {
            if ( e.LeftButton == MouseButtonState.Pressed ) {
                if ( this.WindowState == WindowState.Maximized ) this.WindowState = WindowState.Normal;

                try {
                    DragMove();
                } catch (Exception exception) {
                    Console.WriteLine( exception );
                }
            }
        }

        private void trvMenu_MouseDown(object sender, MouseButtonEventArgs e) { }

        private void trvMenu_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if ( this.TreeControl.SelectedItem != null )
                try {
                    this.Handler.SetCurrentPath( ( (TreePathItem) this.TreeControl.SelectedItem ).Path + "\\" );
                    this._currentExplorerView.List( this.Handler.GetCurrentPath() );
                } catch {
                    // ignored
                }
        }

        private void trvMenu_Expanded(object sender, RoutedEventArgs e) {
            if ( e.OriginalSource is TreeViewItem tvi ) {
                var node = tvi.DataContext as TreePathItem;
                //var node = tvi;

                //MessageBox.Show( string.Format( "TreeNode '{0}' was expanded", tvi.Header ) );
                if ( node == null ) return;

                try {
                    node.Items.Clear();
                    string[] x;

                    try {
                        this.Handler.SetCurrentPath( node.Path + "\\" );

                        x = this.Handler.ListDirectory( this.Handler.GetCurrentPath() );
                    } catch (Exception exception) {
                        x = new[] { exception.Message };
                    }

                    if ( x != null )
                        for ( var i = 0; i < x.Length; i++ ) {
                            //var  pos  = x[i].LastIndexOf( "\\", StringComparison.Ordinal );
                            //var  name = x[i].Substring( pos + 1 );
                            //Item item = new Item( Path.GetFileName( x[i].Substring( pos + 1 ) ), , "", FileType.Directory );

                            TreePathItem n1;

                            try {
                                n1 = new TreePathItem( new DirectoryInfo( x[i] ) );

                                try {
                                    if ( this.Handler.ListDirectory( this.Handler.GetCurrentPath() ) is string[] xJ )
                                        if ( xJ.Length > 0 )
                                            n1.Items.Add( TreePathItem.Empty );
                                    //for ( var j = 0; j < xJ.Length; j++ ) {
                                    //    var n2 = new TreePathItem() { Name = xJ[j], PathAbs = xJ[j] };
                                    //    n1.Items.Add( n2 );
                                    //}
                                } catch (Exception exception) {
                                    var tcs = TreePathItem.Empty;
                                    tcs.Name = exception.Message;

                                    n1.Items.Add( tcs );
                                }
                            } catch {
                                n1      = TreePathItem.Empty;
                                n1.Name = x[i];
                            }

                            node.Items.Add( n1 );
                        }
                } catch {
                    // ignored
                }
            }
        }

        private void trvMenu_Collapsed(object sender, RoutedEventArgs e) { }


        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            EnableBlur();
            //InitializeComponent();
            //(this.listView1.View as GridView)
            //this.listView1.Columns.Add( "Name", 200, System.Windows.Forms.HorizontalAlignment.Left );
            //this.listView1.Columns.Add( "Path", 200, System.Windows.Forms.HorizontalAlignment.Left );
            //this.listView1.Columns.Add( "Size", 70,  System.Windows.Forms.HorizontalAlignment.Left );
            //this.listView1.Columns.Add( "Type", -2,  System.Windows.Forms.HorizontalAlignment.Left );

            //this.listBrowderView.Nodes.Add( "C:\\" );

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

        private ExplorerView CreateExplorer(string path = "/") {
            var h = new LocalHandler( path );
            var x = new ExplorerView( new WindowInteropHelper( this ).Handle, this.consoleX );
        #if PerformanceTest
            var t = new Thread( () => {
        #endif
            x.Init( h );

            x.SendDirectoryUpdateAsCmd += XOnSendDirectoryUpdateAsCmd;
        #if PerformanceTest
            } );
            t.Start();
        #endif
            x.Margin = new Thickness( 0, 0, 0, 0 );
            return x;
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

        private void AddTab(string path = "/") {
            if ( this._contextMenu == null ) CreateContextMenu();

            AddTabToTabControl( CreateExplorerTab( CreateExplorer( path ) ) );
        }

        private void CloseTap(TabItem tp) {
            if ( tp.Content is ExplorerView explorer ) {
                explorer.Dispose();
                tp.Content = null;
                this.TabControl.Items.Remove( tp );
                GC.Collect( 0, GCCollectionMode.Forced );
            }
        }

        #endregion

    }
}
