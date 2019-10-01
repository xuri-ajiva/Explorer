using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Windows.Forms;
using Explorer.Properties;

namespace Explorer {
    public partial class Form1 : Form {
        private readonly IHandler _handler;

        //public static    string   CurrentDirectory = "C:\\";
        public const string TYPE_DIR  = "Directory";
        public const string TYPE_FILE = "File";

        public Form1(IHandler _handler) {
            this._handler = _handler;
            InitializeComponent();
            this.listView1.View       = View.Details;
            this.listView1.CheckBoxes = false;
            this.listView1.GridLines  = true;
            this.listView1.Sorting    = SortOrder.Ascending;
            this.listView1.Columns.Add( "Name", 200, HorizontalAlignment.Left );
            this.listView1.Columns.Add( "Path", 200, HorizontalAlignment.Left );
            this.listView1.Columns.Add( "Size", 70,  HorizontalAlignment.Left );
            this.listView1.Columns.Add( "Type", -2,  HorizontalAlignment.Left );

            //this.listBrowderView.Nodes.Add( "C:\\" );
            var i = 0;
            foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":\\" ).Where( _handler.DirectoryExists ) ) {
                this.listBrowderView.Nodes.Add( dir );

                this.listBrowderView.Nodes[i].Nodes.Add( "empty" );
                //var e = new TreeViewEventArgs( this.listBrowderView.Nodes[i] );
                //treeView1_AfterExpand( null, e );
                i++;
            }

            this._ct = new ContextMenu( new[] { NewDialog() } );
        }

        private MenuItem NewDialog() {
            var subitems = new[] { new MenuItem( "Folder", CoreateFolder ), new MenuItem( "File", CreateFile ) };
            return new MenuItem( "New", subitems );
        }

        private void CreateFile(object sender, EventArgs e) {
            var dir = new GetString( "FileName With Extention Name" );
            if ( dir.ShowDialog() == DialogResult.OK ) {
                this._handler.CreateFile( this._handler.GetCurrentPath(), dir.outref );
                List( this._handler.GetCurrentPath() );
            }
        }

        private void CoreateFolder(object sender, EventArgs e) {
            var dir = new GetString( "Directory Name" );
            if ( dir.ShowDialog() == DialogResult.OK ) {
                _handler.CreateDirectory( this._handler.GetCurrentPath(), dir.outref );
                List( this._handler.GetCurrentPath() );
            }
        }

        //private void button1_Click(object sender, EventArgs e) {
        //    this._handler.SetCurrentPath( @"C:\" );
        //    List( this._handler.GetCurrentPath() );
        //}

        private void List(string dirToScan) {
            this._handler.ValidatePath();
            var count = 0;
            this.listView1.Items.Clear();
            count = Add_Parent_Dir( count );
            _handler.ValidatePath();
            count = List_Dir( dirToScan, count );
            count = List_Files( dirToScan, count );
            if ( this.StatusLabel.ForeColor == Color.DarkGreen ) this.StatusLabel.Text = Resources.Form1_List_CurrentDirectory__ + this._handler.GetCurrentPath();
        }

        private void ProcrestreeView(string dirToList) {
            this.listBrowderView.Nodes.Add( "C:\\" );
            if ( Scan_Dir( dirToList ) is string[] tI )
                for ( var i = 0; i < tI.Length; i++ ) {
                    this.listBrowderView.Nodes[0].Nodes.Add( tI[i] );
                    if ( Scan_Dir( tI[i] ) is string[] tJ )
                        for ( var j = 0; j < tJ.Length; j++ )
                            this.listBrowderView.Nodes[0].Nodes[i].Nodes.Add( tJ[j] );
                }
        }

        private int Add_Parent_Dir(int count) {
            var item = new ListViewItem( "..", count );
            item.SubItems.Add( ".." );
            item.SubItems.Add( "" );
            item.SubItems.Add( TYPE_DIR );
            this.listView1.Items.Add( item );
            return count + 1;
        }

        private void listView1_DoubleClick(object sender, EventArgs e) {
            if ( this.listView1.SelectedItems[0].SubItems[3].Text == TYPE_DIR ) {
                this._handler.SetCurrentPath( this._handler.GetCurrentPath() + this.listView1.SelectedItems[0].Text + @"\" );
                _handler.ValidatePath();
                List( this._handler.GetCurrentPath() );
            }
            else {
                try {
                    this._handler.OpenFile( this.listView1.SelectedItems[0].SubItems[1].Text );
                } catch {
                    // ignored
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
                    var item = new ListViewItem( Path.GetFileName( dirs[i - count] ), i );
                    item.SubItems.Add( dirs[i - count] );
                    item.SubItems.Add( "" );
                    //type
                    item.SubItems.Add( TYPE_DIR );
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
                    var item = new ListViewItem( Path.GetFileName( files[i - count] ), i );
                    item.SubItems.Add( files[i - count] );
                    item.SubItems.Add( GetFileLenght( files[i - count] ) );

                    //type
                    item.SubItems.Add( TYPE_FILE );
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
            this.StatusLabel.ForeColor = Color.DarkRed;
            if ( state ) this.StatusLabel.ForeColor = Color.DarkGreen;
            this.StatusLabel.Text = status;
        }


        private void Form1_Load(object sender, EventArgs e) { this.button2_Click( null, null ); }

        private void treeView1_DoubleClick(object sender, EventArgs e) {
            try {
                this._handler.SetCurrentPath( this.listBrowderView.SelectedNode.Text + "\\" );
                List( this._handler.GetCurrentPath() );
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

        private readonly ContextMenu _ct;

        private void ListView1_MouseClick(object sender, MouseEventArgs e) {
            if ( e.Button == MouseButtons.Right ) this._ct.Show( this.listView1, e.Location );
        }

        private void button2_Click(object sender, EventArgs e) {
            this.listView1.Items.Clear();
            this.listBrowderView.Nodes.Clear();

            var i = 0;
            foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":\\" ).Where( _handler.DirectoryExists ) ) {
                var item = new ListViewItem( dir.Substring( 0, 2 ) );
                item.SubItems.Add( dir );
                item.SubItems.Add( "" );
                item.SubItems.Add( TYPE_DIR );
                this.listView1.Items.Add( item );

                this.listBrowderView.Nodes.Add( dir );
                this.listBrowderView.Nodes[i].Nodes.Add( "empty" );
                //var e = new TreeViewEventArgs( this.listBrowderView.Nodes[i] );
                //treeView1_AfterExpand( null, e );
                i++;
            }

            this._handler.SetCurrentPath( "" );
        }
    }
}