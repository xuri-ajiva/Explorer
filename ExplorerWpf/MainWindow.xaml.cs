﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ConsoleControlAPI;
using ExplorerBase.Handlers;
using ExplorerBase.UI;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.Forms.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Path = System.IO.Path;

namespace ExplorerWpf {
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
        }


        public class Item {
            public string Name { get; set; }

            public string Path { get; set; }

            public string   Size { get; set; }
            public FileType Type { get; set; }

            public Item(string name, string path, string size, FileType type) {
                this.Name = name;
                this.Path = path;
                this.Size = size;
                this.Type = type;
            }
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

        public enum FileType {
            Directory, File
        }

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

            foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":\\" ).Where( handler.DirectoryExists ) ) {
                //TODO:this.listBrowderView.Nodes.Add( dir );
                //TODO:
                //TODO:this.listBrowderView.Nodes[i].Nodes.Add( "empty" );
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
            //TODO:this.listBrowderView.Nodes.Add( "C:\\" );

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
            var  p    = this._handler.GetCurrentPath();
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


        private void treeView1_DoubleClick(object sender, EventArgs e) {
            try {
                //TODO:this._handler.SetCurrentPath( this.listBrowderView.SelectedNode.Text + "\\" );
                //TODO:List( this._handler.GetCurrentPath() );
            } catch { }
        }

        private void treeView1_Click(object sender, EventArgs e) { }

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

            foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":\\" ).Where( this._handler.DirectoryExists ) ) {
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
    }
}