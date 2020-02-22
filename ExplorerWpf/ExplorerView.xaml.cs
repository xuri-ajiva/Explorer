#region using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ExplorerWpf.Handler;
using Point = System.Drawing.Point;

#endregion

namespace ExplorerWpf {
    public sealed partial class ExplorerView : UserControl, IDisposable {
        private readonly DriveInfo[] _devInfo;
        private readonly IntPtr      _hWnd;

        private readonly List<Item> _list = new List<Item>();

        private readonly bool _useListOnly = false;

        private bool _lastError;

        private GridViewColumn freePB;
        private TabItem        myTab;

        public ExplorerView(IntPtr hWnd) {
            this._devInfo = DriveInfo.GetDrives();

            this._hWnd = hWnd;
            InitializeComponent();
            if ( this._useListOnly ) this.DataContext          = this._list;
            if ( this._useListOnly ) this.MainView.DataContext = this._list;
        }

        public IHandler Handler { [DebuggerStepThrough] get; private set; }

        public ObservableCollection<Item> DataCollection => new ObservableCollection<Item>( GetList() );

        #region IDisposable

        /// <inheritdoc />
        public void Dispose() {
            this.Root.Children.Clear();
            this.Root = null;
            //this._handler?.Close();
            this.Handler = null;
        }

        #endregion

        public void Init(IHandler handler) {
            this.Handler             =  handler;
            handler.OnError          += HandlerOnOnError;
            handler.OnSetCurrentPath += HandlerOnOnSetCurrentPath;

            this.freePB = ( (GridView) this.MainView.View ).Columns[4];

            if ( this.Dispatcher != null ) this.Dispatcher.Invoke( () => { List( this.Handler.GetCurrentPath() ); } );
            else
                try {
                    List( this.Handler.GetCurrentPath() );
                } catch {
                    // ignored
                }
        }

        private void HandlerOnOnSetCurrentPath(string arg1, string arg2) {
            if ( this.myTab == null ) return;

            try {
                string d;
                d = arg2 == SettingsHandler.ROOT_FOLDER ? new DirectoryInfo( arg2 ).Name : "Root";
                d = string.IsNullOrEmpty( d ) ? "Explorer" : d;
                Debug.WriteLine( d );
                this.myTab.Dispatcher.Invoke( () => { this.myTab.Header = d; } );
            } catch (Exception e) {
                this.Handler.ThrowError( e );
            }
        }

        private void HandlerOnOnError(Exception obj) {
            new Thread( () => { MessageBox.Show( "module: " + obj.Source + "\n\n" + obj.Message + "\n" + obj.InnerException + "\n" + obj.HelpLink, "Error from FileHandler: " + obj.HResult, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification ); } ).Start();
            this._lastError = true;
            Set_Status( obj.ToString(), false );
        }

        private void BrowseAction(object sender, MouseButtonEventArgs e) {
            if ( e.RightButton == MouseButtonState.Pressed ) return;

            if ( this.MainView.SelectedItems.Count <= 0 ) return;

            var item = this.MainView.SelectedItems[0] as Item;

            if ( item != null && item.Type == Item.FileType.DIRECTORY ) {
                if ( this.Handler.GetCurrentPath() == SettingsHandler.ROOT_FOLDER )
                    if ( item.Path.Length >= 2 )
                        if ( SettingsHandler.ConsoleAutoChangeDisc )
                            OnDirectoryUpdate( item.Path.Substring( 0, 2 ) );

                this.Handler.SetCurrentPath( item.Path + ( item.Path == SettingsHandler.ROOT_FOLDER ? "" : @"\" ) );

                List( this.Handler.GetCurrentPath() );
            }
            else {
                try {
                    if ( item != null )
                        if ( SettingsHandler.ConsoleAutoChangePath )
                            OnDirectoryUpdate( "\"" + item.Path + "\"", false );
                } catch (Exception ex) {
                    MessageBox.Show( ex.Message );
                }
            }
        }

        public event Action<object, string, bool>  SendDirectoryUpdateAsCmd;
        public event Action<object, string, Brush> UpdateStatusBar;


        private void Button_Click(object sender, RoutedEventArgs e) { ListDiscs(); }

        private void MainView_OnMouseDown(object sender, MouseButtonEventArgs e) {
            if ( e.RightButton != MouseButtonState.Pressed ) return;
        }

        private void MainView_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if ( this.MainView.SelectedItems.Count == 0 ) return;

            var wpfP = PointToScreen( e.GetPosition( this ) );

            var p = new Point( (int) wpfP.X, (int) wpfP.Y );

            var dirs  = this.MainView.SelectedItems.Cast<Item>().Where( l => l.Type == Item.FileType.DIRECTORY ).Select( l => l.TryGetDirectoryInfo ).ToArray();
            var files = this.MainView.SelectedItems.Cast<Item>().Where( l => l.Type == Item.FileType.FILE ).Select( l => l.TryGetFileInfo ).ToArray();

            if ( dirs.Any() && files.Any() ) return;

            if ( files.Any() ) this.Handler.ShowContextMenu( files,    p );
            else if ( dirs.Any() ) this.Handler.ShowContextMenu( dirs, p );
        }

        private void SortableListViewColumnHeaderClicked(object sender, RoutedEventArgs e) {
            if ( sender is SortableListView sl ) sl.GridViewColumnHeaderClicked( e.OriginalSource as GridViewColumnHeader );
        }

        private void PathBar_OnKeyDown(object sender, KeyEventArgs e) {
            if ( e.Key != Key.Enter ) return;

            this.Handler.SetCurrentPath( ( sender as TextBox )?.Text );
            List( this.Handler.GetCurrentPath() );
        }

        [DebuggerStepThrough] private void OnUpdateStatusBar(string e, Brush c) { this.UpdateStatusBar?.Invoke( this, e, c ); }

        [DebuggerStepThrough] private void OnDirectoryUpdate(string e, bool cd = true) { this.SendDirectoryUpdateAsCmd?.Invoke( this, e, cd ); }

        #region ListHandeling

        private void AddList(Item item) {
            if ( this._useListOnly )
                this._list.Add( item );
            else
                this.MainView.Items.Add( item );
        }

        private void ClearList() {
            if ( this._useListOnly )
                this._list.Clear();
            else
                this.MainView.Items.Clear();
        }

        private List<Item> GetList() {
            if ( this._useListOnly )
                return this._list;

            return this.MainView.Items.Cast<Item>().ToList();
        }

        #endregion

    #if DEBUG
        ~ExplorerView() { Console.WriteLine( "Destroyed Items: " + DestroyCount++ ); }

        public static int DestroyCount;

    #endif


        #region List

        private void List_Dir(string dirToList) {
            if ( this._lastError ) return;

            var subDirectory = this.Handler.ListDirectory( dirToList );

            foreach ( var directoryInfo in subDirectory ) {
                var item = new Item( directoryInfo );

                AddList( item );
            }
        }

        private void List_Files(string dirToList) {
            if ( this._lastError ) return;

            var subFiles = this.Handler.ListFiles( dirToList );

            foreach ( var fileInfo in subFiles ) {
                var item = new Item( fileInfo );
                AddList( item );
            }
        }


        private void Add_Parent_Dir() {
            var pt   = DirUp( this.Handler.GetCurrentPath() );
            var item = pt == SettingsHandler.ROOT_FOLDER ? Item.Root : new Item( new DirectoryInfo( pt ) );

            item.Name = SettingsHandler.ParentDirectoryPrefix + item.Name;

            AddList( item );
        }

        public string DirUp(string path) {
            if ( path == SettingsHandler.ROOT_FOLDER )
                return SettingsHandler.ROOT_FOLDER;

            var p = this.Handler.GetCurrentPath();

            if ( path.Length <= 3 && Regex.IsMatch( path, @"[A-Za-z]:" ) ) {
                path = SettingsHandler.ROOT_FOLDER;
            }
            else {
                this.Handler.SetCurrentPath( path + "\\..\\" );
                this.Handler.ValidatePath();
                path = this.Handler.GetCurrentPath();
            }

            this.Handler.SetCurrentPath( p );
            return path;
        }

        public void ListP(string dirToScan, bool noCd = false) { this.Dispatcher?.Invoke( () => { List( dirToScan, noCd ); } ); }

        private void List(string dirToScan, bool noCd = false) {
            if ( dirToScan == SettingsHandler.ROOT_FOLDER ) {
                ListDiscs();
                return;
            }

            if ( this._pb ) {
                ( (GridView) this.MainView.View ).Columns.First( x => (string) x.Header == (string) this.freePB.Header ).Width = 0;

                this._pb = false;
            }

            this.Handler.ValidatePath();

            try {
                this._lastError = false;

                Set_Status( "Online", true );
                ClearList();
                Add_Parent_Dir();
                this.Handler.ValidatePath();
                List_Dir( dirToScan );
                List_Files( dirToScan );

                if ( this._lastError ) {
                    this._lastError = false;
                    return;
                }

                var p = this.Handler.GetCurrentPath();
                if ( !noCd && p.Length > 3 )
                    if ( SettingsHandler.ConsoleAutoChangePath )
                        OnDirectoryUpdate( "cd \"" + p + "\"" );

                Set_Status( "Listed: " + p, true );
                SetPath( p );
            } catch (Exception e) {
                Set_Status( e.Message, false );
            }
        }

        #endregion

        #region status

        [DebuggerStepThrough]
        private void Set_Status(string status, bool state) {
            OnUpdateStatusBar( status, state ? Brushes.DarkGreen : Brushes.DarkRed );
            //this.StatusLabel.Foreground = state ? Brushes.DarkGreen : Brushes.DarkRed;
            //this.StatusLabel.Content = status;     
            if ( !state ) this._lastError = true;
        }


        private bool _pb;

        private void ListDiscs() {
            try {
                ClearList();
            } catch (Exception e) {
                this.Handler.ThrowError( e );
            }

            foreach ( var driveInfo in this._devInfo ) {
                //Item item = new Item( string.IsNullOrEmpty( driveInfo.VolumeLabel ) ? "Local Disk(Not Named)" : driveInfo.VolumeLabel, driveInfo.Name, GetLength( driveInfo.AvailableFreeSpace ) + " / " + GetLength( driveInfo.TotalSize ), FileType.Directory );

                var i = new Item( new DirectoryInfo( driveInfo.Name ) ) {
                    Name   = string.IsNullOrEmpty( driveInfo.VolumeLabel ) ? "Local Disk (NotNamed)" : driveInfo.VolumeLabel,
                    Size   = $"{Item.GetLength( driveInfo.AvailableFreeSpace )} free of {Item.GetLength( driveInfo.TotalSize )} ~ {(double) ( driveInfo.TotalSize - driveInfo.AvailableFreeSpace ) / driveInfo.TotalSize * 100D:00.000} %",
                    SizePb = (double) ( driveInfo.TotalSize - driveInfo.AvailableFreeSpace ) / driveInfo.TotalSize * 100D
                };

                AddList( i );

                ( (GridView) this.MainView.View ).Columns.First( x => (string) x.Header == (string) this.freePB.Header ).Width = 100;

                this._pb = true;
            }

            SetPath( SettingsHandler.ROOT_FOLDER );

            this.Handler.SetCurrentPath( "" );
        }

        private void SetPath(string path) {
            this.PathBar.Text = path;
            if ( this.PathBar.Popup != null )
                this.PathBar.Popup.IsOpen = false;
        }

        #endregion


        public void setTapItem(TabItem tabItem) { this.myTab = tabItem; }
    }

    public class SortableListView : ListView {
        private ListSortDirection    _lastDirection = ListSortDirection.Ascending;
        private GridViewColumnHeader _lastHeaderClicked;

        public void GridViewColumnHeaderClicked(GridViewColumnHeader clickedHeader) {
            ListSortDirection direction;

            if ( clickedHeader == null ) return;

            if ( clickedHeader.Role == GridViewColumnHeaderRole.Padding ) return;

            if ( clickedHeader != this._lastHeaderClicked ) direction = ListSortDirection.Ascending;
            else direction                                            = this._lastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;

            if ( clickedHeader.Column.DisplayMemberBinding == null ) return;

            var sortString = ( (Binding) clickedHeader.Column.DisplayMemberBinding ).Path.Path;

            Sort( sortString, direction );

            this._lastHeaderClicked = clickedHeader;
            this._lastDirection     = direction;
        }

        private void Sort(string sortBy, ListSortDirection direction) {
            var dataView = CollectionViewSource.GetDefaultView( this.ItemsSource ?? this.Items );

            dataView.SortDescriptions.Clear();
            var sD = new SortDescription( sortBy, direction );
            dataView.SortDescriptions.Add( sD );
            dataView.Refresh();
        }
    }

}
