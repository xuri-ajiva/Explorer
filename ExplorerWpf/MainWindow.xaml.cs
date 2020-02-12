using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ConsoleControlAPI;
using ExplorerBase.Handlers;
using ExplorerBase.UI;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ContextMenu = System.Windows.Forms.ContextMenu;
using FontStyle = System.Windows.FontStyle;
using Image = System.Drawing.Image;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.Forms.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Path = System.IO.Path;

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

            if ( type == FileType.File ) {
                try {
                    Icon = GetFileIconCashed( Path, IconSize.SHGFI_SMALLICON );
                } catch (Exception e) {
                    Console.WriteLine( e.Message );
                }
            }
        }

        private const Int32 MAX_PATH                = 260;
        private const Int32 SHGFI_ICON              = 0x100;
        private const Int32 SHGFI_USEFILEATTRIBUTES = 0x10;
        private const Int32 FILE_ATTRIBUTE_NORMAL   = 0x80;

        private struct SHFILEINFO {
            public IntPtr hIcon;
            public Int32  iIcon;
            public Int32  dwAttributes;

            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = MAX_PATH )]
            public string szDisplayName;

            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 80 )]
            public string szTypeName;
        }

        public enum IconSize {
            SHGFI_LARGEICON = 0,
            SHGFI_SMALLICON = 1
        }

        [DllImport( "shell32.dll", CharSet = CharSet.Auto )]
        private static extern IntPtr SHGetFileInfo(string pszPath, Int32 dwFileAttributes, ref SHFILEINFO psfi, Int32 cbFileInfo, Int32 uFlags);

        [DllImport( "user32.dll", SetLastError = true )]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // get associated icon (as bitmap).
        private Bitmap GetFileIcon(string fileExt, IconSize ICOsize = IconSize.SHGFI_SMALLICON) {
            SHFILEINFO shinfo = new SHFILEINFO();
            shinfo.szDisplayName = new string( (char) 0, MAX_PATH );
            shinfo.szTypeName    = new string( (char) 0, 80 );
            SHGetFileInfo( fileExt, FILE_ATTRIBUTE_NORMAL, ref shinfo, Marshal.SizeOf( shinfo ), SHGFI_ICON | (int) ICOsize | SHGFI_USEFILEATTRIBUTES );
            Bitmap bmp = System.Drawing.Icon.FromHandle( shinfo.hIcon ).ToBitmap();
            DestroyIcon( shinfo.hIcon ); // must destroy icon to avoid GDI leak!
            return bmp;                  // return icon as a bitmap
        }

        private static readonly Dictionary<string, Bitmap> _smallIconCache = new Dictionary<string, Bitmap>();
        private static readonly Dictionary<string, Bitmap> _largeIconCache = new Dictionary<string, Bitmap>();

        public Bitmap GetFileIconCashed(string fileName, IconSize ICOsize = IconSize.SHGFI_SMALLICON) {
            var extension = System.IO.Path.GetExtension( fileName );
            if ( extension == null )
                return null;

            var cache = ICOsize == IconSize.SHGFI_LARGEICON ? _largeIconCache : _smallIconCache;

            Bitmap icon;
            if ( cache.TryGetValue( extension, out icon ) )
                return icon;

            icon = GetFileIcon( fileName, ICOsize );
            cache.Add( extension, icon );
            return icon;
        }
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
            consoleX.FontStyle           = new FontStyle();

            Init( new LocalHandler( "C:\\" ) );
            EnableBlur();

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

        private void CloseClick(object sender, RoutedEventArgs e) { this.Close(); }

        private void MaxClick(object sender, RoutedEventArgs e) { this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal; }

        private void MinClick(object sender, RoutedEventArgs e) {
            //if ( WindowState == WindowState.Normal ) 
            WindowState = WindowState.Minimized;
        }

        private void PingClick(object sender, RoutedEventArgs e) { this.Topmost = !Topmost; }

        private void MoveWindow(object sender, MouseButtonEventArgs e) { this.DragMove(); }

        ExplorerClass        c;
        private LocalHandler le = new LocalHandler( "C:\\" );

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            //System.Windows.Forms.Integration.WindowsFormsHost host =
            //    new System.Windows.Forms.Integration.WindowsFormsHost();
            //
            //c = new ExplorerClass();
            //
            //this.le.OnSetCurrentPath += Update;
            //this.le.OnSetRemotePath  += Update;
            //
            //this.c.Init( le );
            //this.c.Dock = DockStyle.Fill;
            //host.Child  = this.c;
            //this.grid1.Children.Add( host );
        }

        private void Update() { this.consoleX.ProcessInterface.WriteInput( "cd \"" + le.GetCurrentPath() + "\"" ); }

        #region Explorer

        private ContextMenu _ct;
        private IHandler    _handler;

        private bool _abs = true;

        public event Action<string> PathUpdate;

        public void Init(IHandler handler) {
            if ( handler.GetType() == typeof(NullHandler) ) return;

            this._handler = handler;
            //InitializeComponent();
            //(this.listView1.View as GridView)
            //this.listView1.Columns.Add( "Name", 200, System.Windows.Forms.HorizontalAlignment.Left );
            //this.listView1.Columns.Add( "Path", 200, System.Windows.Forms.HorizontalAlignment.Left );
            //this.listView1.Columns.Add( "Size", 70,  System.Windows.Forms.HorizontalAlignment.Left );
            //this.listView1.Columns.Add( "Type", -2,  System.Windows.Forms.HorizontalAlignment.Left );

            //this.listBrowderView.Nodes.Add( "C:\\" );
            var i = 0;

            foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":" ).Where( handler.DirectoryExists ) ) {
                TreePathItem node = new TreePathItem() { Name = dir, PathAbs = dir };
                node.Items.Add( new TreePathItem { Name       = "empty" } );
                this.trvMenu.Items.Add( node );
                //var e = new TreeViewEventArgs( this.listBrowderView.Nodes[i] );
                //treeView1_AfterExpand( null, e );
                i++;
            }

            this._ct = new ContextMenu( new[] { NewDialog() } );

            this.button2_Click( null, null );
        }


        private MenuItem NewDialog() {
            var subitems = new[] { new MenuItem( "Folder", CoreateFolder ), new MenuItem( "File", CreateFile ) };
            return new MenuItem( "New", subitems );
        }

        private void CreateFile(object sender, EventArgs e) {
            var dir = new GetString( "FileName With Extention Name" );

            if ( dir.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
                this._handler.CreateFile( this._handler.GetCurrentPath() + dir.outref );
                List( this._handler.GetCurrentPath() );
            }

            //MessageBox.Show( "not supported" );
        }

        private void CoreateFolder(object sender, EventArgs e) {
            var dir = new GetString( "Directory Name" );

            if ( dir.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
                this._handler.CreateDirectory( this._handler.GetCurrentPath() + dir.outref );
                List( this._handler.GetCurrentPath() );
            }

            //MessageBox.Show( "not supported" );
        }

        private void List(string dirToScan, bool noCd = false) {
            this._handler.ValidatePath();
            var count = 0;
            this.listView1.Items.Clear();
            count = Add_Parent_Dir( count );
            this._handler.ValidatePath();
            count = List_Dir( dirToScan, count );
            count = List_Files( dirToScan, count );

            if ( this.StatusLabel.Foreground == Brushes.DarkGreen ) {
                if ( !noCd ) this.consoleX.ProcessInterface.WriteInput( "cd \"" + this._handler.GetCurrentPath() + "\"" );
                this.StatusLabel.Content = ( "CurrentDirectory: " + this._handler.GetCurrentPath() );
            }
        }

        private void ProcrestreeView(string dirToList) {
            //TODO:TreePathItem node = new TreePathItem() { Name = dir, PathAbs = dir};
            //TODO:node.Items.Add( new TreePathItem{Name         = "empty"} );
            //TODO:this.trvMenu.Items.Add( node );
            //TODO:this.listBrowderView.Nodes.Add( "C:\\" );
            //TODO:
            //TODO:if ( Scan_Dir( dirToList ) is string[] tI )
            //TODO:    for ( var i = 0; i < tI.Length; i++ ) {
            //TODO:        this.listBrowderView.Nodes[0].Nodes.Add( tI[i] );
            //TODO:        if ( Scan_Dir( tI[i] ) is string[] tJ )
            //TODO:            for ( var j = 0; j < tJ.Length; j++ )
            //TODO:                this.listBrowderView.Nodes[0].Nodes[i].Nodes.Add( tJ[j] );
            //TODO:    }
        }

        private int Add_Parent_Dir(int count) {
            var pt = this._handler.GetCurrentPath();
            this._handler.SetCurrentPath( this._handler.GetCurrentPath() + "\\..\\" );
            this._handler.ValidatePath();
            var p = this._handler.GetCurrentPath();

            if ( p[p.Length - 1] == '\\' ) {
                p = p.Substring( 0, p.Length - 1 );
            }

            Item item = new Item( "..", p, "", FileType.Directory );
            this.listView1.Items.Add( item );
            this._handler.SetCurrentPath( pt );
            this._handler.ValidatePath();
            return count + 1;
        }

        private void listView1_DoubleClick(object sender, EventArgs e) {
            if ( listView1.SelectedItems.Count > 0 ) {
                var item = listView1.SelectedItems[0] as Item;

                if ( item.Type == FileType.Directory ) {
                    //if ( this._abs ) {
                    //    this._handler.SetCurrentPath( item.Name + @"\" );
                    //    this._abs = false;
                    //}
                    //else {
                    //    this._handler.SetCurrentPath( item.Path + @"\" );
                    //    this._handler.ValidatePath();
                    //}
                    this._handler.SetCurrentPath( item.Path + @"\" );

                    List( this._handler.GetCurrentPath() );
                }
                else {
                    try {
                        this._handler.OpenFile( item.Path );
                    } catch (Exception ex) {
                        MessageBox.Show( ex.Message );
                    }
                }
            }
        }

        [DebuggerStepThrough]
        private string[] Scan_Dir(string dirToScan) {
            try {
                Set_Status( "online", true );
                return this._handler.ListDirectory( dirToScan );
            } catch (Exception e) {
                Set_Status( e.Message, false );
                return null;
            }
        }

        private int List_Dir(string dirToList, int count) {
            if ( Scan_Dir( dirToList ) is string[] dirs ) {
                for ( var i = count; i < dirs.Length + count; i++ ) {
                    Item item = new Item( Path.GetFileName( dirs[i - count] ), dirs[i - count], "", FileType.Directory );

                    this.listView1.Items.Add( item );
                }

                return count + dirs.Length;
            }

            return count;
        }

        [DebuggerStepThrough]
        private string[] Scan_Files(string dirToScan) {
            try {
                Set_Status( "online", true );
                return this._handler.ListFiles( dirToScan );
            } catch (Exception e) {
                Set_Status( e.Message, false );
                return null;
            }
        }

        private int List_Files(string dirToList, int count) {
            if ( Scan_Files( dirToList ) is string[] files ) {
                for ( var i = count; i < files.Length + count; i++ ) {
                    Item item = new Item( Path.GetFileName( files[i - count] ), files[i - count], GetFileLenght( files[i - count] ), FileType.File );
                    this.listView1.Items.Add( item );
                }

                return count + files.Length;
            }

            return count;
        }

        [DebuggerStepThrough]
        private string GetFileLenght(string fileName) {
            var length = new FileInfo( fileName ).Length;

            if ( length > Math.Pow( 10, 15 ) ) return ( length / Math.Pow( 10, 15 ) ).ToString( "0.00" ) + "Pb";
            if ( length > Math.Pow( 10, 12 ) ) return ( length / Math.Pow( 10, 12 ) ).ToString( "0.00" ) + "Tb";
            if ( length > Math.Pow( 10, 9 ) ) return ( length / Math.Pow( 10, 9 ) ).ToString( "0.00" )   + "Gb";
            if ( length > Math.Pow( 10, 6 ) ) return ( length / Math.Pow( 10, 6 ) ).ToString( "0.00" )   + "Mb";
            if ( length > Math.Pow( 10, 3 ) ) return ( length / Math.Pow( 10, 3 ) ).ToString( "0.00" )   + "Kb";

            return length + "b";
        }

        [DebuggerStepThrough]
        private void Set_Status(string status, bool state) {
            this.StatusLabel.Foreground = Brushes.DarkRed;
            if ( state ) this.StatusLabel.Foreground = Brushes.DarkGreen;
            this.StatusLabel.Content = status;
        }


        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e) {
            try {
                e.Node.Nodes.Clear();
                this._handler.SetCurrentPath( e.Node.Text + "\\" );
                var x = Scan_Dir( this._handler.GetCurrentPath() );

                if ( x != null )
                    for ( var i = 0; i < x.Length; i++ ) {
                        e.Node.Nodes.Add( x[i] );
                        if ( Scan_Dir( this._handler.GetCurrentPath() ) is string[] xJ )
                            for ( var j = 0; j < xJ.Length; j++ )
                                e.Node.Nodes[i].Nodes.Add( xJ[j] );
                    }

                e.Node.Expand();
            } catch { }
        }

        private void treeView1_AfterCollapse(object sender, TreeViewEventArgs e) {
            try {
                e.Node.Nodes.Clear();
                e.Node.Nodes.Add( "Loding.." );
            } catch { }
        }

        private void ListView1_MouseClick(object sender, MouseEventArgs e) {
            //TODO:if ( e.Button == MouseButtons.Right ) this._ct.Show( this.listView1, e.Location );
        }

        private void button2_Click(object sender, EventArgs e) {
            //TODO:this.listView1.Items.Clear();
            //TODO:this.listBrowderView.Nodes.Clear();

            var i = 0;

            foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":" ).Where( this._handler.DirectoryExists ) ) {
                Item item = new Item( dir.Substring( 0, 2 ), dir, "", FileType.Directory );

                this.listView1.Items.Add( item );

                //TODO:this.listBrowderView.Nodes.Add( dir );
                //TODO:this.listBrowderView.Nodes[i].Nodes.Add( "empty" );
                //var e = new TreeViewEventArgs( this.listBrowderView.Nodes[i] );
                //treeView1_AfterExpand( null, e );
                i++;
            }

            this._abs = true;
            this._handler.SetCurrentPath( "" );
        }

        protected virtual void OnPathUpdate(string obj) { this.PathUpdate?.Invoke( obj ); }

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
            accent.AccentState = AccentState.ACCENT_ENABLE_GRADIENT;

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


        private void listView1_MouseDoubleClick(object sender, MouseButtonEventArgs e) { listView1_DoubleClick( sender, e ); }

        private void trvMenu_MouseDown(object sender, MouseButtonEventArgs e) { }

        private void trvMenu_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if ( this.trvMenu.SelectedItem != null )
                try {
                    this._handler.SetCurrentPath( ( (TreePathItem) this.trvMenu.SelectedItem ).PathAbs + "\\" );
                    List( this._handler.GetCurrentPath() );
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
                    var x = Scan_Dir( this._handler.GetCurrentPath() );

                    if ( x != null )
                        for ( var i = 0; i < x.Length; i++ ) {
                            var pos  = x[i].LastIndexOf( "\\", StringComparison.Ordinal );
                            var name = x[i].Substring( pos + 1 );
                            var n1   = new TreePathItem() { Name = name, PathAbs = x[i] };

                            if ( Scan_Dir( this._handler.GetCurrentPath() ) is string[] xJ ) {
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


    }
}
