using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ExplorerBase.Handlers;
using ExplorerBase.UI;
using Peter;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using UserControl = System.Windows.Controls.UserControl;

namespace ExplorerWpf {
    /// <summary>
    /// Interaktionslogik für ExplorerView.xaml
    /// </summary>
    public partial class ExplorerView : UserControl, IDisposable {
        private readonly IntPtr _hWnd;

        public  bool        InitDone = false;
        private ContextMenu _ct;
        private IHandler    _handler;

        public IHandler Handler {
            [DebuggerStepThrough] get => this._handler;
        }

        public ExplorerView(IntPtr hWnd) {
            this._hWnd = hWnd;
            InitializeComponent();

            // this.MainView.DataContext = this;
        }
    #if DEBUG
        ~ExplorerView() { Console.WriteLine( "Destroyed Items: " + DestroyCount++ ); }

        public static int DestroyCount = 0;

    #endif
        public void Init(IHandler handler) {
            if ( handler.GetType() == typeof(NullHandler) ) return;

            this._handler = handler;
            //this._handler.OnSetCurrentPath += HandlerOnOnSetCurrentPath;

            this._ct = new ContextMenu( new[] { NewDialog() } );

            if ( this.Dispatcher != null )
                Dispatcher.Invoke( () => {
                    List( this._handler.GetCurrentPath(), false );

                    this.InitDone = true;
                } );
            else {
                try {
                    List( this._handler.GetCurrentPath(), false );
                } catch { }

                this.InitDone = true;
            }
        }

        public ObservableCollection<Item> DataCollection => new ObservableCollection<Item>( this.MainView.Items.Cast<Item>() );


        private void HandlerOnOnSetCurrentPath(string arg1, string arg2) {
            if ( arg1 == "" ) {
                OnDirectoryUpdate( arg2.Substring( 0, 2 ) );
                return;
            }

            if ( arg2.Length > 1 && arg1.Length > 1 ) {
                if ( !string.Equals( arg1.Substring( 0, 2 ), arg2.Substring( 0, 2 ), StringComparison.CurrentCultureIgnoreCase ) ) {
                    OnDirectoryUpdate( arg2.Substring( 0, 2 ) );
                }
            }
        }

        private System.Windows.Forms.MenuItem NewDialog() {
            var subitems = new[] { new System.Windows.Forms.MenuItem( "Folder", CoreateFolder ), new System.Windows.Forms.MenuItem( "File", CreateFile ) };
            return new MenuItem( "New", subitems );
        }


        #region FileAndDirectroryCreaTE

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

        #endregion

        private void BrowseAction(object sender, MouseButtonEventArgs e) {
            if ( e.RightButton == MouseButtonState.Pressed ) {
                return;
            }

            if ( this.MainView.SelectedItems.Count > 0 ) {
                var item = this.MainView.SelectedItems[0] as Item;

                if ( item.Type == FileType.Directory ) {
                    //if ( this._abs ) {
                    //    this._handler.SetCurrentPath( item.Name + @"\" );
                    //    this._abs = false;
                    //}
                    //else {
                    //    this._handler.SetCurrentPath( item.Path + @"\" );
                    //    this._handler.ValidatePath();
                    //}

                    if ( this._handler.GetCurrentPath() == "/" ) {
                        if ( item.Path.Length >= 2 )
                            OnDirectoryUpdate( item.Path.Substring( 0, 2 ) );
                    }

                    this._handler.SetCurrentPath( item.Path + ( item.Path == "/" ? "" : @"\" ) );

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

        #region List

        [DebuggerStepThrough]
        public string[] Scan_Dir(string dirToScan) {
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

                    this.MainView.Items.Add( item );
                }

                return count + dirs.Length;
            }

            return count;
        }

        [DebuggerStepThrough]
        public string[] Scan_Files(string dirToScan) {
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
                    this.MainView.Items.Add( item );
                }

                return count + files.Length;
            }

            return count;
        }


        private int Add_Parent_Dir(int count) {
            var pt = this._handler.GetCurrentPath();

            if ( pt.Length <= 3 && Regex.IsMatch( pt, @"[A-Za-z]:" ) ) {
                pt = "/";
            }
            else {
                this._handler.SetCurrentPath( this._handler.GetCurrentPath() + "\\..\\" );
                this._handler.ValidatePath();
                pt = this._handler.GetCurrentPath();

                if ( pt[pt.Length - 1] == '\\' ) {
                    pt = pt.Substring( 0, pt.Length - 1 );
                }
            }

            Item item = new Item( "..", pt, "", FileType.Directory );
            this.MainView.Items.Add( item );
            this._handler.SetCurrentPath( pt );
            this._handler.ValidatePath();
            return count + 1;
        }


        public void List(string dirToScan, bool noCd = false) {
            if ( dirToScan == "/" ) {
                ListDiscs();
                return;
            }

            this._handler.ValidatePath();
            var count = 0;
            this.MainView.Items.Clear();
            count = Add_Parent_Dir( count );
            this._handler.ValidatePath();
            count = List_Dir( dirToScan, count );
            count = List_Files( dirToScan, count );

            if ( this.StatusLabel.Foreground == Brushes.DarkGreen ) {
                var p = this._handler.GetCurrentPath();
                if ( !noCd && ( p.Length > 3 ) )
                    OnDirectoryUpdate( "cd \"" + p + "\"" );

                //TODO:!if ( !noCd ) this.consoleX.ProcessInterface.WriteInput( "cd \"" + this._handler.GetCurrentPath() + "\"" );
                this.StatusLabel.Content = ( "CurrentDirectory: " + p );
            }
        }

        #endregion


        public event EventHandler<string> SendDirectoryUpdateAsCmd;

        #region status

        [DebuggerStepThrough]
        private string GetFileLenght(string fileName) { return GetLenght( new FileInfo( fileName ).Length ); }

        private string GetLenght(long length) {
            if ( length > Math.Pow( 10, 15 ) ) return ( length / Math.Pow( 10, 15 ) ).ToString( "0.00" ) + "Pb";
            if ( length > Math.Pow( 10, 12 ) ) return ( length / Math.Pow( 10, 12 ) ).ToString( "0.00" ) + "Tb";
            if ( length > Math.Pow( 10, 9 ) ) return ( length / Math.Pow( 10, 9 ) ).ToString( "0.00" )   + "Gb";
            if ( length > Math.Pow( 10, 6 ) ) return ( length / Math.Pow( 10, 6 ) ).ToString( "0.00" )   + "Mb";
            if ( length > Math.Pow( 10, 3 ) ) return ( length / Math.Pow( 10, 3 ) ).ToString( "0.00" )   + "Kb";

            return length + "b";
        }

        [DebuggerStepThrough]
        public void Set_Status(string status, bool state) {
            this.StatusLabel.Foreground = Brushes.DarkRed;
            if ( state ) this.StatusLabel.Foreground = Brushes.DarkGreen;
            this.StatusLabel.Content = status;
        }

        public void ListDiscs() {
            //TODO:this.listView1.Items.Clear();
            //TODO:this.listBrowderView.Nodes.Clear();
            try {
                MainView.Items.Clear();
            } catch (Exception e) {
                Console.WriteLine( e );
            }

            foreach ( var driveInfo in DriveInfo.GetDrives() ) {
                Item item = new Item( string.IsNullOrEmpty( driveInfo.VolumeLabel ) ? "Local Disk(Not Named)" : driveInfo.VolumeLabel, driveInfo.Name, GetLenght( driveInfo.AvailableFreeSpace ) + " / " + GetLenght( driveInfo.TotalSize ), FileType.Directory );

                this.MainView.Items.Add( item );
            }

            //foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":" ).Where( this._handler.DirectoryExists ) ) {
            //    Item item = new Item( dir.Substring( 0, 2 ), dir, "", FileType.Directory );
            //
            //    this.MainView.Items.Add( item );
            //
            //    //TODO:this.listBrowderView.Nodes.Add( dir );
            //    //TODO:this.listBrowderView.Nodes[i].Nodes.Add( "empty" );
            //    //var e = new TreeViewEventArgs( this.listBrowderView.Nodes[i] );
            //    //treeView1_AfterExpand( null, e );
            //    i++;
            //}

            this._handler.SetCurrentPath( "" );
        }

        #endregion

        protected virtual void OnDirectoryUpdate(string e) { this.SendDirectoryUpdateAsCmd?.Invoke( this, e ); }

        private void Button_Click(object sender, RoutedEventArgs e) { ListDiscs(); }

        #region IDisposable

        /// <inheritdoc />
        public void Dispose() {
            this.Root.Children.Clear();
            this.Root = null;
            //this._handler?.Close();
            this._handler = null;
            this._ct?.Dispose();
        }

        #endregion


        private void MainView_OnMouseDown(object sender, MouseButtonEventArgs e) {
            if ( e.RightButton != MouseButtonState.Pressed ) return;
        }


        private void MainView_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if ( this.MainView.SelectedItems.Count == 0 ) return;

            //var itemX = (Item) this.MainView.SelectedItem;

            var wpfP = PointToScreen( e.GetPosition( this ) );

            var p = new System.Drawing.Point( (int) wpfP.X, (int) wpfP.Y );

            ShellContextMenu ctxMnu = new ShellContextMenu();

            bool                       oneFile = false;
            Dictionary<FileType, bool> test    = new Dictionary<FileType, bool>();

            List<Item> list = new List<Item>();

            foreach ( Item yi in this.MainView.SelectedItems ) {
                if ( test.ContainsKey( yi.Type ) ) {
                    continue;
                }

                if ( test.Keys.Count > 0 ) {
                    oneFile = true;
                    //break;
                }

                if ( yi.Path.Length > 3 ) {
                    list.Add( yi );
                }
            }

            if ( oneFile ) {
                list = new List<Item>();
                list.Add( ( (Item) this.MainView.SelectedItems[0] ) );
            }

            //var list = oneFile ? new[] { this.MainView.SelectedItems[0] } : this.MainView.SelectedItems;
            if ( list.Count <= 0 ) return;

            switch (( (Item) list[0] ).Type) {
                case FileType.File: {
                    FileInfo[] arrFI = new FileInfo[list.Count];

                    for ( var i = 0; i < list.Count; i++ ) {
                        arrFI[i] = new FileInfo( ( (Item) list[i] ).Path );
                    }

                    ctxMnu.ShowContextMenu( arrFI, p );
                    break;
                }
                case FileType.Directory: {
                    DirectoryInfo[] arrFI = new DirectoryInfo[list.Count];

                    for ( var i = 0; i < list.Count; i++ ) {
                        arrFI[i] = new DirectoryInfo( ( (Item) list[i] ).Path );
                    }

                    ctxMnu.ShowContextMenu( arrFI, p );
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void SortableListViewColumnHeaderClicked(object sender, RoutedEventArgs e) {
            var sl = ( sender as SortableListView );
            sl.GridViewColumnHeaderClicked( e.OriginalSource as GridViewColumnHeader );
        }
    }

    public partial class SortableListView : System.Windows.Controls.ListView {
        private GridViewColumnHeader lastHeaderClicked = null;
        private ListSortDirection    lastDirection     = ListSortDirection.Ascending;

        public void GridViewColumnHeaderClicked(GridViewColumnHeader clickedHeader) {
            ListSortDirection direction;

            if ( clickedHeader != null ) {
                if ( clickedHeader.Role != GridViewColumnHeaderRole.Padding ) {
                    if ( clickedHeader != lastHeaderClicked ) {
                        direction = ListSortDirection.Ascending;
                    }
                    else {
                        if ( lastDirection == ListSortDirection.Ascending ) {
                            direction = ListSortDirection.Descending;
                        }
                        else {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string sortString = ( (Binding) clickedHeader.Column.DisplayMemberBinding ).Path.Path;

                    Sort( sortString, direction );

                    lastHeaderClicked = clickedHeader;
                    lastDirection     = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction) {
            ICollectionView dataView = CollectionViewSource.GetDefaultView( this.ItemsSource != null ? this.ItemsSource : this.Items );

            dataView.SortDescriptions.Clear();
            SortDescription sD = new SortDescription( sortBy, direction );
            dataView.SortDescriptions.Add( sD );
            dataView.Refresh();
        }
    }

}
