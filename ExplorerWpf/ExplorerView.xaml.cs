#region using

using ExplorerWpf.Handler;
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
using Point = System.Drawing.Point;

#endregion

namespace ExplorerWpf {
    public partial class ExplorerView : UserControl, IDisposable {
        private readonly DriveInfo[]                       _devInfo;
        private readonly IntPtr                            _hWnd;

        private readonly List<Item> _list = new List<Item>();

        private bool _lastError;

        private readonly bool           _useListOnly = false;
        private          GridViewColumn freePB;

        public ExplorerView(IntPtr hWnd) {
            this._devInfo = DriveInfo.GetDrives();

            this._hWnd           = hWnd;
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
            this.Handler         =  handler;
            this.Handler.OnError += HandlerOnOnError;

            this.freePB = ( (GridView) this.MainView.View ).Columns[4];

            if ( this.Dispatcher != null ) this.Dispatcher.Invoke( () => { List( this.Handler.GetCurrentPath() ); } );
            else
                try {
                    List( this.Handler.GetCurrentPath() );
                } catch {
                    // ignored
                }
        }

        private void HandlerOnOnError(Exception obj) {
            new Thread( () => { MessageBox.Show( "module: " + obj.Source + "\n\n" + obj.Message + "\n" + obj.InnerException + "\n" + obj.HelpLink, "Error from FileHandler: " + obj.HResult, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification ); } ).Start();
            this._lastError = true;
            Set_Status( obj.ToString(), false );
            Console.WriteLine( obj );
        }
        
        private void BrowseAction(object sender, MouseButtonEventArgs e) {
            if ( e.RightButton == MouseButtonState.Pressed ) return;

            if ( this.MainView.SelectedItems.Count <= 0 ) return;

            var item = this.MainView.SelectedItems[0] as Item;

            if ( item != null && item.Type == Item.FileType.DIRECTORY ) {
                if ( this.Handler.GetCurrentPath() == "/" )
                    if ( item.Path.Length >= 2 )
                        OnDirectoryUpdate( item.Path.Substring( 0, 2 ) );

                this.Handler.SetCurrentPath( item.Path + ( item.Path == "/" ? "" : @"\" ) );

                List( this.Handler.GetCurrentPath() );
            }
            else {
                try {
                    if ( item != null ) OnDirectoryUpdate(  "\"" + item.Path + "\"" );
                } catch (Exception ex) {
                    MessageBox.Show( ex.Message );
                }
            }
        }

        public event EventHandler<string>          SendDirectoryUpdateAsCmd;
        public event Action<object, string, Brush> UpdateStatusBar;

        protected virtual void OnDirectoryUpdate(string e) { this.SendDirectoryUpdateAsCmd?.Invoke( this, e ); }

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

        private void PathBar_OnKeyDown(object sender, KeyEventArgs e) {
            if ( e.Key != Key.Enter ) return;

            this.Handler.SetCurrentPath( ( sender as TextBox )?.Text );
            List( this.Handler.GetCurrentPath() );
        }

        protected virtual void OnUpdateStatusBar(string e, Brush c) { this.UpdateStatusBar?.Invoke( this, e, c ); }

    #if DEBUG
        ~ExplorerView() { Console.WriteLine( "Destroyed Items: " + DestroyCount++ ); }

        public static int DestroyCount;

    #endif


        #region List

        private void List_Dir(string dirToList) {
            var subDirectory = this.Handler.ListDirectory( dirToList );

            foreach ( var directoryInfo in subDirectory ) {
                var item = new Item( directoryInfo );

                AddList( item );
            }
        }

        private void List_Files(string dirToList) {
            var subFiles = this.Handler.ListFiles( dirToList );

            foreach ( var fileInfo in subFiles ) {
                var item = new Item( fileInfo );
                AddList( item );
            }
        }


        private void Add_Parent_Dir() {
            var pt = this.Handler.GetCurrentPath();
            var p  = pt;

            if ( pt.Length <= 3 && Regex.IsMatch( pt, @"[A-Za-z]:" ) ) {
                pt = "/";
            }
            else {
                this.Handler.SetCurrentPath( this.Handler.GetCurrentPath() + "\\..\\" );
                this.Handler.ValidatePath();
                pt = this.Handler.GetCurrentPath();
            }

            var item = pt == "/" ? Item.Root : new Item( new DirectoryInfo( pt ) );

            item.Name = "!(..)!: " + item.Name;

            AddList( item );
            this.Handler.SetCurrentPath( p );
        }


        public void List(string dirToScan, bool noCd = false) {
            if ( dirToScan == "/" ) {
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
                    Console.WriteLine( "Error On List" );
                    this._lastError = false;
                    return;
                }

                var p = this.Handler.GetCurrentPath();
                if ( !noCd && p.Length > 3 )
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
                Console.WriteLine( e );
            }

            foreach ( var driveInfo in this._devInfo ) {
                //Item item = new Item( string.IsNullOrEmpty( driveInfo.VolumeLabel ) ? "Local Disk(Not Named)" : driveInfo.VolumeLabel, driveInfo.Name, GetLenght( driveInfo.AvailableFreeSpace ) + " / " + GetLenght( driveInfo.TotalSize ), FileType.Directory );

                var i = new Item( new DirectoryInfo( driveInfo.Name ) );
                i.Size   = $"{Item.GetLenght( driveInfo.AvailableFreeSpace )} free of {Item.GetLenght( driveInfo.TotalSize )} ~ {(double) ( driveInfo.TotalSize - driveInfo.AvailableFreeSpace ) / driveInfo.TotalSize * 100D:00.000} %";
                i.SizePb = (double) ( driveInfo.TotalSize - driveInfo.AvailableFreeSpace ) / driveInfo.TotalSize * 100D;

                AddList( i );

                ( (GridView) this.MainView.View ).Columns.First( x => (string) x.Header == (string) this.freePB.Header ).Width = 100;

                this._pb = true;
            }

            SetPath( "/" );

            this.Handler.SetCurrentPath( "" );
        }

        private void SetPath(string path) {
            this.PathBar.Text = path;
            if ( this.PathBar.Popup != null )
                this.PathBar.Popup.IsOpen = false;
        }

        #endregion

    }

    public class SortableListView : ListView {
        private ListSortDirection    _lastDirection = ListSortDirection.Ascending;
        private GridViewColumnHeader _lastHeaderClicked;

        public void GridViewColumnHeaderClicked(GridViewColumnHeader clickedHeader) {
            ListSortDirection direction;

            if ( clickedHeader == null ) return;

            if ( clickedHeader.Role == GridViewColumnHeaderRole.Padding ) return;

            if ( clickedHeader != this._lastHeaderClicked ) {
                direction = ListSortDirection.Ascending;
            }
            else {
                direction = this._lastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }

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
