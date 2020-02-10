#region using

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ConsoleControlAPI;
using ExplorerBase;
using ExplorerBase.Handlers;

#endregion

namespace ExplorerBase.UI {
    public partial class ExplorerClass : UserControl {
        //public static    string   CurrentDirectory = "C:\\";
        public const string TYPE_DIR  = "Directory";
        public const string TYPE_FILE = "File";

        private ContextMenu _ct;
        private IHandler    _handler;

        private bool _abs = true;

        public ExplorerClass() { }

        public event Action<string> PathUpdate;

        public void Init(IHandler handler) {
            this.BackgroundImage  = new ErrorProvider().Icon.ToBitmap();
            BackgroundImageLayout = ImageLayout.Center;
            if ( handler.GetType() == typeof(NullHandler) ) return;

            this._handler = handler;
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

            foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":\\" ).Where( handler.DirectoryExists ) ) {
                this.listBrowderView.Nodes.Add( dir );

                this.listBrowderView.Nodes[i].Nodes.Add( "empty" );
                //var e = new TreeViewEventArgs( this.listBrowderView.Nodes[i] );
                //treeView1_AfterExpand( null, e );
                i++;
            }

            this._ct = new ContextMenu( new[] { NewDialog() } );

            this.button2_Click( null, null );

            this.consoleX.StartProcess( "cmd.exe", "" );
            this.consoleX.ProcessInterface.Process.OutputDataReceived += ProcessOnOutputDataReceived;
            this.consoleX.ProcessInterface.OnProcessOutput            += ConsoleXOnOnProcessOutput;
            this.consoleX.ProcessInterface.OnProcessInput             += ConsoleXOnOnProcessInput;
            this.consoleX.InternalRichTextBox.KeyDown                 += InternalRichTextBoxOnKeyDown;
            this.consoleX.IsInputEnabled                              =  true;
            this.consoleX.Visible                                     =  false;
        }


        private void InternalRichTextBoxOnKeyDown(object sender, KeyEventArgs e) {
            if ( e.KeyCode == Keys.Return ) {
                this.nextIn = true;
            }
        }

        private MenuItem NewDialog() {
            var subitems = new[] { new MenuItem( "Folder", CoreateFolder ), new MenuItem( "File", CreateFile ) };
            return new MenuItem( "New", subitems );
        }

        private void CreateFile(object sender, EventArgs e) {
            var dir = new GetString( "FileName With Extention Name" );

            if ( dir.ShowDialog() == DialogResult.OK ) {
                this._handler.CreateFile( this._handler.GetCurrentPath() + dir.outref );
                List( this._handler.GetCurrentPath() );
            }

            //MessageBox.Show( "not supported" );
        }

        private void CoreateFolder(object sender, EventArgs e) {
            var dir = new GetString( "Directory Name" );

            if ( dir.ShowDialog() == DialogResult.OK ) {
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

            if ( this.StatusLabel.ForeColor == Color.DarkGreen ) {
                if ( !noCd ) this.consoleX.ProcessInterface.WriteInput( "cd \"" + this._handler.GetCurrentPath() + "\"" );
                this.StatusLabel.Text = "CurrentDirectory: " + this._handler.GetCurrentPath();
            }
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
                if ( this._abs ) {
                    this._handler.SetCurrentPath( this.listView1.SelectedItems[0].Text + @"\" );
                    this._abs = false;
                }
                else {
                    this._handler.SetCurrentPath( this._handler.GetCurrentPath() + this.listView1.SelectedItems[0].Text + @"\" );
                    this._handler.ValidatePath();
                }

                List( this._handler.GetCurrentPath() );
            }
            else {
                try {
                    this._handler.OpenFile( this.listView1.SelectedItems[0].SubItems[1].Text );
                } catch (Exception ex) {
                    MessageBox.Show( ex.Message );
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
                    item.SubItems.Add( /*GetFileLenght( files[i - count] )*/"" );

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


        private void Form1_Load(object sender, EventArgs e) {
            //List( this._handler.GetCurrentPath() );
        }

        private bool first = false;

        private bool nextIn = false;

        private void ConsoleXOnOnProcessInput(object sender, ProcessEventArgs args) {
            if ( args.Content.StartsWith( "cd" ) ) {
                this.nextIn = true;
            }
        }

        private void ConsoleXOnOnProcessOutput(object sender, ProcessEventArgs args) {
            if ( !this.first ) {
                new Thread( () => {
                    Thread.Sleep( 1000 );

                    this.Invoke( new Action( () => { this.consoleX.ProcessInterface.WriteInput( "@echo off" ); } ) );
                    Thread.Sleep( 100 );
                    this.Invoke( new Action( () => {
                        this.consoleX.ClearOutput();
                        this.consoleX.WriteOutput( "Console Support Enabled!\n", Color.FromArgb( 0, 129, 255 ) );
                        this.consoleX.Visible = true;
                    } ) );
                } ); //.Start();

                this.consoleX.Visible = true;
                this.first            = true;
            }

            if ( args.Code.HasValue ) {
                this.consoleX.WriteOutput( $"[{args.Code.Value}]", Color.DarkBlue );
            }

            //if ( Regex.IsMatch( args.Content, @"[A-Z]:\\[^>]*>" ) ) {
            this.consoleX.WriteOutput( "> ", Color.Yellow );
            this.consoleX.WriteOutput( " ",  Color.DeepSkyBlue );

            /*if ( this.nextIn ) {

                //List( args.Content.Substring( 0, args.Content.Length-1 ) );
                this._handler.SetCurrentPath(args.Content.Substring( 0, args.Content.Length -1 ).Replace( "\n","" ).Replace( "\r","" ) + "\\" );
                List( this._handler.GetCurrentPath() );

                this.nextIn = false;
            }*/

            //}
            var march = Regex.Match( args.Content, @"[A-Z]:\\[^>]*>" );

            if ( march.Success ) {
                this._handler.SetCurrentPath( march.Value.Substring( 0, march.Value.Length - 1 ).Replace( "\n", "" ).Replace( "\r", "" ) + "\\" );
                List( this._handler.GetCurrentPath(),true );

                this.nextIn = false;
            }
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e) {
            if ( Regex.IsMatch( e.Data, @"[A-Z]:\\[^>]*>" ) ) {
                this._handler.SetCurrentPath( e.Data.Substring( 0, e.Data.Length - 1 ).Replace( "\n", "" ).Replace( "\r", "" ) + "\\" );
                List( this._handler.GetCurrentPath() );

                this.nextIn = false;
            }
        }

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

        private void ListView1_MouseClick(object sender, MouseEventArgs e) {
            if ( e.Button == MouseButtons.Right ) this._ct.Show( this.listView1, e.Location );
        }

        private void button2_Click(object sender, EventArgs e) {
            this.listView1.Items.Clear();
            this.listBrowderView.Nodes.Clear();

            var i = 0;

            foreach ( var dir in "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select( c => c + ":\\" ).Where( this._handler.DirectoryExists ) ) {
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

            this._abs = true;
            this._handler.SetCurrentPath( "" );
        }

        protected virtual void OnPathUpdate(string obj) { this.PathUpdate?.Invoke( obj ); }
    }
}
