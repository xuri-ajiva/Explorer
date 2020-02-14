using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
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

namespace ExplorerWpf {

    public class ImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if ( value is Bitmap ) {
                var stream = new MemoryStream();
                ( (Bitmap) value ).Save( stream, ImageFormat.Png );

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.EndInit();

                return bitmap;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) { throw new NotImplementedException(); }
    }
    public class Item {
        public Bitmap Icon { get; }
        public string Name { get; set; }

        public string Path { get; set; }

        public string   Size { get; set; }
        public FileType Type { get; set; }


        public Item(string name, string path, string size, FileType type) {
            this.Name = name;
            this.Path = path.Replace( "\\\\", "\\" );
            this.Size = size;
            this.Type = type;

            try {
                if ( path != "/" ) this.Icon = DefaultIcons1.GetFileIconCashed( this.Path ).ToBitmap();
                else {
                    this.Icon = SystemIcons.Shield.ToBitmap();
                }
            } catch (Exception e) {
                try {
                    this.Icon = SystemIcons.Error.ToBitmap();
                } catch (Exception exception) {
                    Console.WriteLine( exception );
                }

                Console.WriteLine( e.Message );
            }
        }

        public static class DefaultIcons1 {
            private static readonly Dictionary<string, Icon> cache = new Dictionary<string, Icon>();

            public static Icon GetFileIconCashed(string path) {
                string ext = path;

                if ( File.Exists( path ) ) {
                    ext = System.IO.Path.GetExtension( path );
                }

                if ( ext == null )
                    return null;

                Icon icon;
                if ( cache.TryGetValue( ext, out icon ) )
                    return icon;

                icon = ExtractFromPath( path );
                cache.Add( ext, icon );
                return icon;
            }

            private static readonly Lazy<Icon> _lazyFolderIcon = new Lazy<Icon>( FetchIcon, true );

            public static Icon FolderLarge { get { return _lazyFolderIcon.Value; } }

            private static Icon FetchIcon() {
                var tmpDir = Directory.CreateDirectory( System.IO.Path.Combine( System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() ) ).FullName;
                var icon   = ExtractFromPath( tmpDir );
                Directory.Delete( tmpDir );
                return icon;
            }

            private static Icon ExtractFromPath(string path) {
                SHFILEINFO shinfo = new SHFILEINFO();
                SHGetFileInfo( path, 0, ref shinfo, (uint) Marshal.SizeOf( shinfo ), SHGFI_ICON | SHGFI_LARGEICON );
                return System.Drawing.Icon.FromHandle( shinfo.hIcon );
            }

            //Struct used by SHGetFileInfo function
            [StructLayout( LayoutKind.Sequential )]
            private struct SHFILEINFO {
                public IntPtr hIcon;
                public int    iIcon;
                public uint   dwAttributes;

                [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
                public string szDisplayName;

                [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 80 )]
                public string szTypeName;
            };

            [DllImport( "shell32.dll" )]
            private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            private const uint SHGFI_ICON      = 0x100;
            private const uint SHGFI_LARGEICON = 0x0;
            private const uint SHGFI_SMALLICON = 0x000000001;
        }

        public static class DefaultIcons {
            private static Icon folderIcon;

            public static Icon FolderLarge => folderIcon ?? ( folderIcon = GetStockIcon( SHSIID_FOLDER, SHGSI_LARGEICON ) );

            private static Icon GetStockIcon(uint type, uint size) {
                var info = new SHSTOCKICONINFO();
                info.cbSize = (uint) Marshal.SizeOf( info );

                SHGetStockIconInfo( type, SHGSI_ICON | size, ref info );

                var icon = (Icon) System.Drawing.Icon.FromHandle( info.hIcon ).Clone(); // Get a copy that doesn't use the original handle
                DestroyIcon( info.hIcon );                                              // Clean up native icon to prevent resource leak

                return icon;
            }

            [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
            public struct SHSTOCKICONINFO {
                public uint   cbSize;
                public IntPtr hIcon;
                public int    iSysIconIndex;
                public int    iIcon;

                [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
                public string szPath;
            }

            [DllImport( "shell32.dll" )]
            public static extern int SHGetStockIconInfo(uint siid, uint uFlags, ref SHSTOCKICONINFO psii);

            [DllImport( "user32.dll" )]
            public static extern bool DestroyIcon(IntPtr handle);

            private const uint SHSIID_FOLDER   = 0x3;
            private const uint SHGSI_ICON      = 0x100;
            private const uint SHGSI_LARGEICON = 0x0;
            private const uint SHGSI_SMALLICON = 0x1;
        }

        ~Item() { Icon.Dispose(); }
    }
    public enum FileType {
        Directory, File
    }

    public class TreePathItem {

        public string PathAbs { get; set; }

        public TreePathItem() { this.Items = new ObservableCollection<TreePathItem>(); }

        public string Name { get; set; }

        public ObservableCollection<TreePathItem> Items { get; set; }


    }
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        [DllImport( "kernel32" )] private static extern bool AllocConsole();


        private ExplorerView currentExplorerView;
        private IHandler     _handler => this.currentExplorerView.Handler;

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


        private bool first = false;

        private void ConsoleXOnOnProcessInput(object sender, ProcessEventArgs args) { }


        private void ConsoleXOnOnProcessOutput(object sender, ProcessEventArgs args) {
            if ( !this.first ) {
                new Thread( () => {
                    Thread.Sleep( 1000 );

                    var dispatcher = this.Dispatcher;

                    if ( dispatcher != null ) {
                        dispatcher.Invoke( () => { this.consoleX.ProcessInterface.WriteInput( "@echo off" ); } );
                        Thread.Sleep( 100 );
                        dispatcher.Invoke( () => {
                            this.consoleX.ClearOutput();
                            this.consoleX.WriteOutput( "Console Support Enabled!\n", Color.FromRgb( 0, 129, 255 ) );
                            this.consoleX.Visibility = Visibility.Visible;
                        } );
                    }
                    else { }
                } ); //.Start();

                this.consoleX.Visibility = Visibility.Visible;
                this.first               = true;
            }

            if ( args.Code.HasValue ) {
                this.consoleX.WriteOutput( $"[{args.Code.Value}]", Colors.DarkBlue );
            }

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
            if ( this.WindowState == WindowState.Maximized ) {
                this.WindowState = WindowState.Normal;
            }

            DragMove();
        }


        #region Explorer

        #endregion

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

        private void trvMenu_MouseDown(object sender, MouseButtonEventArgs e) { }

        private void trvMenu_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if ( this.TreeControl.SelectedItem != null )
                try {
                    this._handler.SetCurrentPath( ( (TreePathItem) this.TreeControl.SelectedItem ).PathAbs + "\\" );
                    this.currentExplorerView.List( this._handler.GetCurrentPath() );
                } catch { }
        }

        private void trvMenu_Expanded(object sender, RoutedEventArgs e) {
            if ( e.OriginalSource is TreeViewItem tvi ) {
                var node = tvi.DataContext as TreePathItem;

                //MessageBox.Show( string.Format( "TreeNode '{0}' was expanded", tvi.Header ) );
                if ( node == null ) return;

                try {
                    node.Items.Clear();
                    this._handler.SetCurrentPath( node.PathAbs + "\\" );
                    var x = this.currentExplorerView.Scan_Dir( this._handler.GetCurrentPath() );

                    if ( x != null )
                        for ( var i = 0; i < x.Length; i++ ) {
                            var pos  = x[i].LastIndexOf( "\\", StringComparison.Ordinal );
                            var name = x[i].Substring( pos + 1 );
                            var n1   = new TreePathItem() { Name = name, PathAbs = x[i] };

                            if ( this.currentExplorerView.Scan_Dir( this._handler.GetCurrentPath() ) is string[] xJ ) {
                                var n2 = new TreePathItem() { Name = "empty", PathAbs = "empty" };
                                n1.Items.Add( n2 );
                                //for ( var j = 0; j < xJ.Length; j++ ) {
                                //    var n2 = new TreePathItem() { Name = xJ[j], PathAbs = xJ[j] };
                                //    n1.Items.Add( n2 );
                                //}
                            }

                            node.Items.Add( n1 );
                        }
                } catch { }
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
                TreePathItem node = new TreePathItem() { Name = driveInfo.VolumeLabel, PathAbs = driveInfo.Name };
                node.Items.Add( new TreePathItem { Name       = "empty" } );
                this.TreeControl.Items.Add( node );
            }

            ///
            ///
            ///  NetworkHandler
            ///
            /// 
            ///var i = 0;
            ///
            ///foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":" ).Where( this._handler.DirectoryExists ) ) {
            ///    TreePathItem node = new TreePathItem() { Name = dir, PathAbs = dir };
            ///    node.Items.Add( new TreePathItem { Name       = "empty" } );
            ///    this.trvMenu.Items.Add( node );
            ///    //var e = new TreeViewEventArgs( this.listBrowderView.Nodes[i] );
            ///    //treeView1_AfterExpand( null, e );
            ///    i++;
            ///}
            ///

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
            //t.Start();
        }

        private void DCChange(object sender, DependencyPropertyChangedEventArgs e) { Console.WriteLine( e ); }

        private void taps_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if ( this.TabControl.SelectedItem is TabItem tp ) {
                if ( tp.Content != null ) {
                    if ( this.currentExplorerView == null || !this.currentExplorerView.Equals( tp.Content ) ) {
                        if ( tp.Content is ExplorerView explorer && explorer.InitDone ) {
                            this.currentExplorerView = explorer;
                            var p = this._handler.GetCurrentPath();

                            if ( Regex.IsMatch( p, @"[A-Za-z]:\\" ) ) {
                                this.consoleX.ProcessInterface.WriteInput( p.Substring( 0, 2 ) );
                            }

                            if ( ( p.Length > 3 ) )
                                this.consoleX.ProcessInterface.WriteInput( "cd \"" + p + "\"" );
                        }
                    }
                }
                else {
                    if ( this.TabControl.Items.Count > 1 ) {
                        this.TabControl.SelectedIndex = this.TabControl.Items.Count - 2;
                    }
                }
            }
        }


        private ContextMenu contextMenu;

        void CreateContextMenu() {
            if ( this.contextMenu == null ) {
                ContextMenu m = new ContextMenu();

                var me1 = new MenuItem { TabIndex = 100, Header = "_Close" };
                me1.Click += MenuItem_OnClick;

                m.Items.Add( me1 );
                this.contextMenu = m;
            }
        }

        ExplorerView CreateExplorer(string path = "/") {
            LocalHandler h = new LocalHandler( path );
            ExplorerView x = new ExplorerView(new WindowInteropHelper(this).Handle);
            new Thread( () => {
                x.Init( h );

                x.SendDirectoryUpdateAsCmd += XOnSendDirectoryUpdateAsCmd;
            } ).Start();
            return x;
        }

        TabItem CreateExplorerTab(ExplorerView explorerView) {
            TabItem newTabItem = new TabItem {
                Header      = new Label(){Content =  "Explorer", ContextMenu = this.contextMenu},
                Name        = "Explorer",
                Content     = explorerView,
                //Background  = this.ColorExample.Background,
                //Foreground  = this.ColorExample.Foreground,
                //BorderBrush = this.ColorExample.BorderBrush,
               Style =  this.PlusTabItem.Style
            };
            return newTabItem;
        }

        void AddTabToTabControl(TabItem newTabItem) {
            this.TabControl.Items.Add( new TabItem() { Header = "Temp" } );
            var p = this.TabControl.Items[this.TabControl.Items.Count - 2];
            this.TabControl.Items[this.TabControl.Items.Count - 2] = newTabItem;
            this.TabControl.Items[this.TabControl.Items.Count - 1] = p;

            this.TabControl.SelectedIndex = this.TabControl.Items.Count - 2;
        }

        void AddTab(string path = "/") {
            if ( this.contextMenu == null ) {
                CreateContextMenu();
            }

            AddTabToTabControl( CreateExplorerTab( CreateExplorer( path ) ) );
        }

        void CloseTap(TabItem tp) {
            if ( tp.Content is ExplorerView explorer ) {
                explorer.Dispose();
                tp.Content = null;
                this.TabControl.Items.Remove( tp );
                tp = null;
                GC.Collect( 0, GCCollectionMode.Forced );
            }
        }


        private void XOnSendDirectoryUpdateAsCmd(object sender, string e) {
            if ( sender.Equals( this.currentExplorerView ) ) {
                this.consoleX.ProcessInterface.WriteInput( e );
            }
        }

        //private void TabItem_MouseDown(object sender, MouseButtonEventArgs e) {
        //    if ( this.c == null ) {
        //        MenuItem[] items = new MenuItem[2];
        //        items[0] = new MenuItem( "Local Explorer", (o, args) => { AddTab(); } );
        //        items[1] = new MenuItem( "Test",           (o, args) => { MessageBox.Show( "Test" ); } );
        //
        //        c = new ContextMenu( items );
        //    }
        //
        //    var ctr = (Control) sender;
        //
        //    var wpfP = ( ctr.TransformToAncestor( this ).Transform( new Point( 10, 10 ) ) );
        //    var p    = new System.Drawing.Point( Convert.ToInt32( wpfP.X ), Convert.ToInt32( wpfP.Y ) );
        //    this.c.Show( (System.Windows.Forms.Control) sender, p );
        //}

        private void UIElement_OnMouseEnter(object sender, MouseEventArgs e) {
            //MenuS.Visibility = Visibility.Visible; 
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
                    if ( ((ContextMenu) me.Parent ).PlacementTarget is Control parent ) {
                        if ( parent.Parent is TabItem tp && tp.Content != null ) {
                            CloseTap( tp );
                        }
                    }

                    break;
                default:
                    break;
            }
        }


        private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { AddTab(); }

    }
}
