using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ExplorerBase.Handlers;
using ExplorerBase.UI;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using Path = System.IO.Path;

namespace ExplorerWpf {
    /// <summary>
    /// Interaktionslogik für ExplorerView.xaml
    /// </summary>
    public partial class ExplorerView : UserControl {

        private ContextMenu _ct;
        private IHandler    _handler;

        public IHandler Handler {
            [DebuggerStepThrough] get => this._handler;
        }

        public ExplorerView() { InitializeComponent(); }

        public void Init(IHandler handler) {
            if ( handler.GetType() == typeof(NullHandler) ) return;

            this._handler = handler;
            //this._handler.OnSetCurrentPath += HandlerOnOnSetCurrentPath;

            this._ct = new ContextMenu( new[] { NewDialog() } );

            ListDiscs();
        }

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
            this._handler.SetCurrentPath( this._handler.GetCurrentPath() + "\\..\\" );
            this._handler.ValidatePath();
            var p = this._handler.GetCurrentPath();

            if ( p[p.Length - 1] == '\\' ) {
                p = p.Substring( 0, p.Length - 1 );
            }

            Item item = new Item( "..", p, "", FileType.Directory );
            this.MainView.Items.Add( item );
            this._handler.SetCurrentPath( pt );
            this._handler.ValidatePath();
            return count + 1;
        }


        public void List(string dirToScan, bool noCd = false) {
            this._handler.ValidatePath();
            var count = 0;
            this.MainView.Items.Clear();
            count = Add_Parent_Dir( count );
            this._handler.ValidatePath();
            count = List_Dir( dirToScan, count );
            count = List_Files( dirToScan, count );

            if ( this.StatusLabel.Foreground == Brushes.DarkGreen ) {
                if ( !noCd )
                    OnDirectoryUpdate( "cd \"" + this._handler.GetCurrentPath() + "\"" );

                //TODO:!if ( !noCd ) this.consoleX.ProcessInterface.WriteInput( "cd \"" + this._handler.GetCurrentPath() + "\"" );
                this.StatusLabel.Content = ( "CurrentDirectory: " + this._handler.GetCurrentPath() );
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
            MainView.Items.Clear();

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
    }
}
