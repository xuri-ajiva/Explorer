using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Explorer {
    public partial class Form1 : Form {
        public static string currentdir = "C:\\";
        public const  string Type_Dir   = "Directory";
        public const  string Type_File  = "File";

        public Form1() {
            InitializeComponent();
            this.listView1.View       = View.Details;
            this.listView1.CheckBoxes = false;
            this.listView1.GridLines  = true;
            this.listView1.Sorting    = SortOrder.Ascending;
            this.listView1.Columns.Add( "Name", 200, HorizontalAlignment.Left );
            this.listView1.Columns.Add( "Path", 200, HorizontalAlignment.Left );
            this.listView1.Columns.Add( "Size", 70,  HorizontalAlignment.Left );
            this.listView1.Columns.Add( "Type", -2,  HorizontalAlignment.Left );

            this.treeView1.Nodes.Add( "C:\\" );
            TreeViewEventArgs e = new TreeViewEventArgs( this.treeView1.Nodes[0] );
            treeView1_AfterExpand( null, e );

            this.ct = new ContextMenu( new MenuItem[] { NewDialog() } );
        }

        private MenuItem NewDialog() {
            var subitems = new MenuItem[] { new MenuItem( "Folder", CoreateFolder ), new MenuItem( "File", CreateFile ) };
            return new MenuItem( "New", subitems );
        }

        private void CreateFile(object sender, EventArgs e) {
            var dir = new GetString( "FileName With Extention Name" );
            if ( dir.ShowDialog() == DialogResult.OK ) {
                File.Create( currentdir + dir.outref ).Close();
                List( currentdir );
            }
        }

        private void CoreateFolder(object sender, EventArgs e) {
            var dir = new GetString( "Directory Name" );
            if ( dir.ShowDialog() == DialogResult.OK ) {
                Directory.CreateDirectory( currentdir + dir.outref );
                List( currentdir );
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            currentdir = @"C:\";
            List( currentdir );
        }

        private void List(string DirToScan) {
            currentdir = MakePath( currentdir );
            int count = 0;
            this.listView1.Items.Clear();
            count      = Add_Parent_Dir( count );
            currentdir = MakePath( currentdir );
            count      = List_Dir( DirToScan, count );
            count      = List_Files( DirToScan, count );
            if ( this.StatusLabel.ForeColor == Color.DarkGreen ) this.StatusLabel.Text = "CurrentDirectory: " + ( currentdir );
            //ProcrestreeView(currentdir);
        }

        private void ProcrestreeView(string DirToList) {
            this.treeView1.Nodes.Add( "C:\\" );
            bool surc_i = true;
            var  t_i    = Scan_Dir( DirToList, out surc_i );
            if ( surc_i )
                for ( var i = 0; i < t_i.Length; i++ ) {
                    this.treeView1.Nodes[0].Nodes.Add( t_i[i] );
                    bool surc_j = true;
                    var  t_j    = Scan_Dir( t_i[i], out surc_j );
                    if ( surc_j )
                        for ( var j = 0; j < t_j.Length; j++ ) {
                            this.treeView1.Nodes[0].Nodes[i].Nodes.Add( t_j[j] );
                        }
                }
        }

        private int Add_Parent_Dir(int count) {
            ListViewItem item = new ListViewItem( "..", count );
            item.SubItems.Add( ".." );
            item.SubItems.Add( "" );
            item.SubItems.Add( Type_Dir );
            this.listView1.Items.Add( item );
            return count + 1;
        }

        private void listView1_DoubleClick(object sender, EventArgs e) {
            if ( this.listView1.SelectedItems[0].SubItems[3].Text == Type_Dir ) {
                currentdir += this.listView1.SelectedItems[0].Text.ToString() + @"\";
                //currentdir = Path.GetDirectoryName(currentdir);
                List( currentdir );
            }
            else {
                try {
                    Process.Start( this.listView1.SelectedItems[0].SubItems[1].Text );
                } catch { }
            }
        }

        private string[] Scan_Dir(string DirToScan, out bool surc) {
            try {
                Set_Status( "online", true );
                surc = true;
                return Directory.GetDirectories( DirToScan );
            } catch (Exception e) {
                Set_Status( e.Message, false );
                surc = false;
                return null;
            }
        }

        private int List_Dir(string DirToList, int count) {
            bool surc = false;
            var  dirs = Scan_Dir( DirToList, out surc );
            if ( dirs != null && surc ) {
                for ( var i = count; i < dirs.Length + count; i++ ) {
                    ListViewItem item = new ListViewItem( Path.GetFileName( dirs[i - count] ), i );
                    item.SubItems.Add( dirs[i - count] );
                    item.SubItems.Add( "" );
                    //type
                    item.SubItems.Add( Type_Dir );
                    this.listView1.Items.Add( item );
                }

                return count + dirs.Length;
            }

            return count;
        }

        private string[] Scan_Files(string DirToScan) {
            try {
                Set_Status( "online", true );
                return Directory.GetFiles( DirToScan );
            } catch (Exception e) {
                Set_Status( e.Message, false );
                return null;
            }
        }

        private int List_Files(string DirToList, int count) {
            var files = Scan_Files( DirToList );
            if ( files != null ) {
                for ( var i = count; i < files.Length + count; i++ ) {
                    ListViewItem item = new ListViewItem( Path.GetFileName( files[i - count] ), i );
                    item.SubItems.Add( files[i - count] );
                    item.SubItems.Add( GetFileLenght( files[i - count] ) );

                    //type
                    item.SubItems.Add( Type_File );
                    this.listView1.Items.Add( item );
                }

                return count + files.Length;
            }

            return count;
        }

        private string GetFileLenght(string FileName) {
            long length = new FileInfo( FileName ).Length;

            if ( length > Math.Pow( 10, 15 ) ) return ( length / Math.Pow( 10, 15 ) ).ToString( "0.00" ) + "Pb";
            if ( length > Math.Pow( 10, 12 ) ) return ( length / Math.Pow( 10, 12 ) ).ToString( "0.00" ) + "Tb";
            if ( length > Math.Pow( 10, 9 ) ) return ( length / Math.Pow( 10, 9 ) ).ToString( "0.00" )   + "Gb";
            if ( length > Math.Pow( 10, 6 ) ) return ( length / Math.Pow( 10, 6 ) ).ToString( "0.00" )   + "Mb";
            if ( length > Math.Pow( 10, 3 ) )
                return ( length / Math.Pow( 10, 3 ) ).ToString( "0.00" ) + "Kb";
            else
                return length + "b";
        }

        private void Set_Status(string Status, bool state) {
            this.StatusLabel.ForeColor = Color.DarkRed;
            if ( state ) this.StatusLabel.ForeColor = Color.DarkGreen;
            this.StatusLabel.Text = Status;
        }

        private static string MakePath(string Dir) {
            string ret = "";
            var    str = Dir.Split( '\\' );
            int    rm  = 0;
            for ( int i = str.Length - 1; i >= 0; i -= 1 ) {
                if ( str[i] == ".." ) {
                    rm     += 1;
                    str[i] =  "";
                }
                else if ( rm > 0 && !str[i].Contains( ":" ) ) {
                    str[i] =  "";
                    rm     -= 1;
                }
                else {
                    ret += str[i] + @"\";
                }
            }

            var    v     = ret.Split( '\\' );
            string retur = "";
            for ( int i = v.Length - 1; i > 0; i -= 1 ) {
                retur += v[i] + @"\";
            }

            return retur.Substring( 1 );
        }

        private void Form1_Load(object sender, EventArgs e) { button1_Click( null, null ); }

        private void treeView1_DoubleClick(object sender, EventArgs e) {
            try {
                currentdir = this.treeView1.SelectedNode.Text + "\\";
                List( currentdir );
            } catch { }
        }

        private void treeView1_Click(object sender, EventArgs e) { }

        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e) {
            try {
                e.Node.Nodes.Clear();
                currentdir = e.Node.Text + "\\";
                bool surc = true;
                var  x    = Scan_Dir( currentdir, out surc );
                if ( surc )
                    for ( int i = 0; i < x.Length; i++ ) {
                        e.Node.Nodes.Add( x[i] );

                        bool surc_j = true;
                        var  x_j    = Scan_Dir( currentdir, out surc );
                        if ( surc_j )
                            for ( int j = 0; j < x_j.Length; j++ ) {
                                e.Node.Nodes[i].Nodes.Add( x_j[j] );
                            }
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

        private ContextMenu ct;

        private void ListView1_MouseClick(object sender, MouseEventArgs e) {
            if ( e.Button == MouseButtons.Right ) {
                this.ct.Show( this.listView1, e.Location );
            }
        }
    }
}