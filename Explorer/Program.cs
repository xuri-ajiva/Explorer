using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Explorer.RemoteHandler;

namespace Explorer {
    public class Program {
        public const int PORT = 32896;

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
            new Thread( CreateServer ).Start();

            initRemote();

            var t = new Thread( () => {
                Application.Run( new UserInterface( _remoteHandler ) );
            } );
            t.SetApartmentState( ApartmentState.STA );
            t.Start();
        }


        private void CreateServer() {
            LocalHandler my = new LocalHandler();
            TcpListener  ls = new TcpListener( IPAddress.Any, PORT );
            ls.Start();
            var cl     = ls.AcceptTcpClient();
            var buffer = new byte[BUFFER_SIZE];
            while ( cl.Connected ) {
                var enc = new byte[cl.Client.Receive( buffer, SocketFlags.None )];
                Array.Copy( buffer, enc, enc.Length );
                var str     = Decoder( enc );
                var action  = str.Substring( 0, CONTEND_SIZE );
                var contend = str.Substring( CONTEND_SIZE );
                var ret     = "";
                try {
                    switch (action) {
                        case TRUE:
                        case FALSE: break;
                        case DIRECTORY_EXISTS:
                            ret = my.DirectoryExists( contend ) ? TRUE : FALSE;
                            break;
                        case CREATE_DIRECTORY:
                            ret = TRUE;
                            my.CreateDirectory( contend );
                            break;
                        case CREATE_FILE:
                            ret = TRUE;
                            my.CreateFile( contend );
                            break;
                        case DELETE_DIRECTORY:
                            ret = TRUE;
                            my.DeleteDirectory( contend );
                            break;
                        case DELETE_FILE:
                            ret = TRUE;
                            my.DeleteFile( contend );
                            break;
                        case MAKE_PATH:
                            ret = TRUE;
                            my.ValidatePath();
                            break;
                        case GET_DIRECTORYS:
                            ret = To_XML( my.ListDirectory( contend ) );
                            break;
                        case GET_FILES:
                            ret = To_XML( my.ListFiles( contend ) );
                            break;
                        case SET_REMOTE_PATH:
                            ret = TRUE;
                            my.SetCurrentPath( contend );
                            break;
                        case GET_REMOTE_PATH:
                            ret = my.GetCurrentPath();
                            break;
                        default:
                            ret = FALSE;
                            break;
                    }
                } catch (Exception e) {
                    ret = e.Message;
                }

                cl.Client.Send( Encoder( ret ) );
            }
        }

        private void initRemote() {
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