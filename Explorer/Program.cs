using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using ExplorerBase.Handlers;
using ExplorerBase.UI;

namespace Explorer {
    public class Program {
        public const int PORT = 32896;
        private static Thread Server;

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            // var xml = To_XML( new LocalHandler( "C:\\" ).ListDirectory( "C:\\" ) );
            // var enc = Encoder( xml );
            // var dec = Decoder( enc );
            // Console.WriteLine( xml );
            // Console.WriteLine( dec );
            // var nor = From_XML( dec );

            new Program();
        }

        private static IHandler _remoteHandler;

        public Program() {
            initRemote();

            var t = new Thread( () => {
                Application.Run( new UserInterface( _remoteHandler ) );
            } );
            t.SetApartmentState( ApartmentState.STA );
            t.Start();

            //do {
            //    Console.WriteLine( "write stop | StOp | STOP | sTOP | stoP \nTo Stop The Server" );
            //} while ( Console.ReadLine().ToLower() != "stop" );
            //Server.Abort();
        }

        private void initRemote() {
            if ( MessageBox.Show( "Start A Server ?", "", MessageBoxButtons.YesNo ) == DialogResult.Yes ) {
               Server = RemoteHandler.StartServer( PORT );
            }
            var h = new GetString( "Bitte Remote ip Angeben" );
            if ( h.ShowDialog() == DialogResult.OK ) {
                try {
                    TcpClient cl = new TcpClient();
                    cl.Connect( IPAddress.Parse( h.outref /*"127.0.0.1" */ ), PORT );
                    _remoteHandler = new RemoteHandler( cl );
                } catch (Exception e) {
                    MessageBox.Show( e.Message );
                    Environment.Exit( e.HResult );
                }
            }
            else {
                _remoteHandler = new NullHandler();
            }
        }
    }
}