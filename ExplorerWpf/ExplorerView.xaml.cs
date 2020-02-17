#region using

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
using System.Windows.Media;
using ExplorerBase.Handlers;
using Peter;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using Point = System.Drawing.Point;

#endregion

namespace ExplorerWpf {
    /// <summary>
    ///     Interaktionslogik für ExplorerView.xaml
    /// </summary>
    public partial class ExplorerView : UserControl, IDisposable {
        private readonly IntPtr                            _hWnd;
        private readonly ConsoleControl.WPF.ConsoleControl _consoleControl;
        private          ContextMenu                       _ct;

        private readonly DriveInfo[] devInfo;

        public bool InitDone;

        public ExplorerView(IntPtr hWnd, ConsoleControl.WPF.ConsoleControl consoleControl) {
            this.devInfo = DriveInfo.GetDrives();

            this._hWnd           = hWnd;
            this._consoleControl = consoleControl;
            InitializeComponent();

            // this.MainView.DataContext = this;
        }

        public IHandler Handler { [DebuggerStepThrough] get; private set; }

        public ObservableCollection<Item> DataCollection => new ObservableCollection<Item>( this.MainView.Items.Cast<Item>() );

        #region IDisposable

        /// <inheritdoc />
        public void Dispose() {
            this.Root.Children.Clear();
            this.Root = null;
            //this._handler?.Close();
            this.Handler = null;
            this._ct?.Dispose();
        }

        #endregion

        public void Init(IHandler handler) {
            //if ( handler.GetType() == typeof(NullHandler) ) return;

            this.Handler = handler;
            //this._handler.OnSetCurrentPath += HandlerOnOnSetCurrentPath;

            this._ct = new ContextMenu( new[] { NewDialog() } );

            if ( this.Dispatcher != null ) {
                this.Dispatcher.Invoke( () => {
                    List( this.Handler.GetCurrentPath() );

                    this.InitDone = true;
                } );
            }
            else {
                try {
                    List( this.Handler.GetCurrentPath() );
                } catch { }

                this.InitDone = true;
            }
        }


        private void HandlerOnOnSetCurrentPath(string arg1, string arg2) {
            if ( arg1 == "" ) {
                OnDirectoryUpdate( arg2.Substring( 0, 2 ) );
                return;
            }

            if ( arg2.Length > 1 && arg1.Length > 1 )
                if ( !string.Equals( arg1.Substring( 0, 2 ), arg2.Substring( 0, 2 ), StringComparison.CurrentCultureIgnoreCase ) )
                    OnDirectoryUpdate( arg2.Substring( 0,                       2 ) );
        }

        private MenuItem NewDialog() {
            var subitems = new[] { new MenuItem( "Folder", CoreateFolder ), new MenuItem( "File", CreateFile ) };
            return new MenuItem( "New", subitems );
        }

        private void BrowseAction(object sender, MouseButtonEventArgs e) {
            if ( e.RightButton == MouseButtonState.Pressed ) return;

            if ( this.MainView.SelectedItems.Count > 0 ) {
                var item = this.MainView.SelectedItems[0] as Item;

                if ( item.Type == FileType.DIRECTORY ) {
                    //if ( this._abs ) {
                    //    this._handler.SetCurrentPath( item.Name + @"\" );
                    //    this._abs = false;
                    //}
                    //else {
                    //    this._handler.SetCurrentPath( item.Path + @"\" );
                    //    this._handler.ValidatePath();
                    //}

                    if ( this.Handler.GetCurrentPath() == "/" )
                        if ( item.Path.Length >= 2 )
                            OnDirectoryUpdate( item.Path.Substring( 0, 2 ) );

                    this.Handler.SetCurrentPath( item.Path + ( item.Path == "/" ? "" : @"\" ) );

                    List( this.Handler.GetCurrentPath() );
                }
                else {
                    try {
                        this._consoleControl.ProcessInterface.WriteInput( item.Path );
                        //var p = (Process)this.Handler.OpenFile( item.Path );
                        //p.StartInfo.RedirectStandardOutput = true;
                        //p.OutputDataReceived += POnOutputDataReceived;
                    } catch (Exception ex) {
                        MessageBox.Show( ex.Message );
                    }
                }
            }
        }


        public event EventHandler<string> SendDirectoryUpdateAsCmd;

        protected virtual void OnDirectoryUpdate(string e) { this.SendDirectoryUpdateAsCmd?.Invoke( this, e ); }

        private void Button_Click(object sender, RoutedEventArgs e) { ListDiscs(); }


        private void MainView_OnMouseDown(object sender, MouseButtonEventArgs e) {
            if ( e.RightButton != MouseButtonState.Pressed ) return;
        }


        private void MainView_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if ( this.MainView.SelectedItems.Count == 0 ) return;

            //var itemX = (Item) this.MainView.SelectedItem;

            var wpfP = PointToScreen( e.GetPosition( this ) );

            var p = new Point( (int) wpfP.X, (int) wpfP.Y );

            var ctxMnu = new ShellContextMenu();

            var oneFile = false;
            var test    = new Dictionary<FileType, bool>();

            var list = new List<Item>();

            foreach ( Item yi in this.MainView.SelectedItems ) {
                if ( test.ContainsKey( yi.Type ) ) continue;

                if ( test.Keys.Count > 0 ) oneFile = true;
                //break;

                if ( yi.Path.Length > 3 ) list.Add( yi );
            }

            if ( oneFile ) {
                list = new List<Item>();
                list.Add( (Item) this.MainView.SelectedItems[0] );
            }

            //var list = oneFile ? new[] { this.MainView.SelectedItems[0] } : this.MainView.SelectedItems;
            if ( list.Count <= 0 ) return;

            switch (list[0].Type) {
                case FileType.FILE: {
                    var arrFI = new FileInfo[list.Count];

                    for ( var i = 0; i < list.Count; i++ ) arrFI[i] = new FileInfo( list[i].Path );

                    ctxMnu.ShowContextMenu( arrFI, p );
                    break;
                }
                case FileType.DIRECTORY: {
                    var arrFI = new DirectoryInfo[list.Count];

                    for ( var i = 0; i < list.Count; i++ ) arrFI[i] = new DirectoryInfo( list[i].Path );

                    ctxMnu.ShowContextMenu( arrFI, p );
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void SortableListViewColumnHeaderClicked(object sender, RoutedEventArgs e) {
            var sl = sender as SortableListView;
            sl.GridViewColumnHeaderClicked( e.OriginalSource as GridViewColumnHeader );
        }
    #if DEBUG
        ~ExplorerView() { Console.WriteLine( "Destroyed Items: " + DestroyCount++ ); }

        public static int DestroyCount;

    #endif


        #region FileAndDirectroryCreaTE

        private void CreateFile(object sender, EventArgs e) {
            //var dir = new GetString( "FileName With Extention Name" );
            //
            //if ( dir.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
            //    this._handler.CreateFile( this._handler.GetCurrentPath() + dir.outref );
            //    List( this._handler.GetCurrentPath() );
            //}

            MessageBox.Show( "not supported" );
        }

        private void CoreateFolder(object sender, EventArgs e) {
            //var dir = new GetString( "Directory Name" );
            //
            //if ( dir.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
            //    this._handler.CreateDirectory( this._handler.GetCurrentPath() + dir.outref );
            //    List( this._handler.GetCurrentPath() );
            //}

            MessageBox.Show( "not supported" );
        }

        #endregion

        #region List

        [DebuggerStepThrough]
        public string[] Scan_Dir(string dirToScan) {
            try {
                Set_Status( "online", true );
                return this.Handler.ListDirectory( dirToScan );
            } catch (Exception e) {
                Set_Status( e.Message, false );
                return null;
            }
        }

        private int List_Dir(string dirToList, int count) {
            if ( Scan_Dir( dirToList ) is string[] dirs ) {
                for ( var i = count; i < dirs.Length + count; i++ ) {
                    var item = new Item( new DirectoryInfo( dirs[i - count] ) );

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
                return this.Handler.ListFiles( dirToScan );
            } catch (Exception e) {
                Set_Status( e.Message, false );
                return null;
            }
        }

        private int List_Files(string dirToList, int count) {
            if ( Scan_Files( dirToList ) is string[] files ) {
                for ( var i = count; i < files.Length + count; i++ ) {
                    var item = new Item( new FileInfo( files[i - count] ) );
                    this.MainView.Items.Add( item );
                }

                return count + files.Length;
            }

            return count;
        }


        private int Add_Parent_Dir(int count) {
            var pt = this.Handler.GetCurrentPath();

            if ( pt.Length <= 3 && Regex.IsMatch( pt, @"[A-Za-z]:" ) ) {
                pt = "/";
            }
            else {
                this.Handler.SetCurrentPath( this.Handler.GetCurrentPath() + "\\..\\" );
                this.Handler.ValidatePath();
                pt = this.Handler.GetCurrentPath();

                //if ( pt[pt.Length - 1] == '\\' ) {
                //    pt = pt.Substring( 0, pt.Length - 1 );
                //}
            }

            this.MainView.Items.Add( pt == "/" ? Item.Root : new Item( new DirectoryInfo( pt ) ) );
            this.Handler.SetCurrentPath( pt );
            this.Handler.ValidatePath();
            return count + 1;
        }


        public void List(string dirToScan, bool noCd = false) {
            if ( dirToScan == "/" ) {
                ListDiscs();
                return;
            }

            this.Handler.ValidatePath();
            var count = 0;
            this.MainView.Items.Clear();
            count = Add_Parent_Dir( count );
            this.Handler.ValidatePath();
            count = List_Dir( dirToScan, count );
            count = List_Files( dirToScan, count );

            if ( this.StatusLabel.Foreground == Brushes.DarkGreen ) {
                var p = this.Handler.GetCurrentPath();
                if ( !noCd && p.Length > 3 )
                    OnDirectoryUpdate( "cd \"" + p + "\"" );

                this.StatusLabel.Content = "CurrentDirectory: " + p;
            }
        }

        #endregion

        #region status

        [DebuggerStepThrough]
        public void Set_Status(string status, bool state) {
            this.StatusLabel.Foreground = Brushes.DarkRed;
            if ( state ) this.StatusLabel.Foreground = Brushes.DarkGreen;
            this.StatusLabel.Content = status;
        }

        public void ListDiscs() {
            try {
                this.MainView.Items.Clear();
            } catch (Exception e) {
                Console.WriteLine( e );
            }

            foreach ( var driveInfo in this.devInfo ) {
                //Item item = new Item( string.IsNullOrEmpty( driveInfo.VolumeLabel ) ? "Local Disk(Not Named)" : driveInfo.VolumeLabel, driveInfo.Name, GetLenght( driveInfo.AvailableFreeSpace ) + " / " + GetLenght( driveInfo.TotalSize ), FileType.Directory );

                var i = new Item( new DirectoryInfo( driveInfo.Name ) );
                i.Size = ( (double) ( driveInfo.TotalSize - driveInfo.AvailableFreeSpace ) / driveInfo.TotalSize * 100D ).ToString( "00.000" ) + $" % ({Item.GetLenght( driveInfo.TotalSize )})";

                this.MainView.Items.Add( i );
            }

            this.Handler.SetCurrentPath( "" );
        }

        #endregion

    }

    public class SortableListView : ListView {
        private ListSortDirection    lastDirection = ListSortDirection.Ascending;
        private GridViewColumnHeader lastHeaderClicked;

        public void GridViewColumnHeaderClicked(GridViewColumnHeader clickedHeader) {
            ListSortDirection direction;

            if ( clickedHeader != null )
                if ( clickedHeader.Role != GridViewColumnHeaderRole.Padding ) {
                    if ( clickedHeader != this.lastHeaderClicked ) {
                        direction = ListSortDirection.Ascending;
                    }
                    else {
                        if ( this.lastDirection == ListSortDirection.Ascending ) direction = ListSortDirection.Descending;
                        else direction                                                     = ListSortDirection.Ascending;
                    }

                    if(clickedHeader.Column.DisplayMemberBinding == null) return;

                    var sortString = ( (Binding) clickedHeader.Column.DisplayMemberBinding ).Path.Path;

                    Sort( sortString, direction );

                    this.lastHeaderClicked = clickedHeader;
                    this.lastDirection     = direction;
                }
        }

        private void Sort(string sortBy, ListSortDirection direction) {
            var dataView = CollectionViewSource.GetDefaultView( this.ItemsSource != null ? this.ItemsSource : this.Items );

            dataView.SortDescriptions.Clear();
            var sD = new SortDescription( sortBy, direction );
            dataView.SortDescriptions.Add( sD );
            dataView.Refresh();
        }
    }

}
