#region using

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

#endregion

namespace ExplorerBase.Handlers {
    public class RemoteHandler : IHandler {
        public const string TRUE             = "TR";
        public const string FALSE            = "FA";
        public const string DIRECTORY_EXISTS = "EX";
        public const string CREATE_DIRECTORY = "MD";
        public const string CREATE_FILE      = "MF";
        public const string DELETE_DIRECTORY = "DD";
        public const string DELETE_FILE      = "DF";
        public const string MAKE_PATH        = "PA";
        public const string GET_DIRECTORYS   = "Gd";
        public const string GET_FILES        = "Gf";
        public const string SET_REMOTE_PATH  = "Ps";
        public const string GET_REMOTE_PATH  = "Pg";
        public const int    CONTEND_SIZE     = 2;

        public const     int       BUFFER_SIZE = 409600;
        private readonly TcpClient _cl;
        protected        byte[]    buffer;

        public RemoteHandler(TcpClient cl) {  
            this._cl         = cl;
            this.CurrentPath = "";          
            this.OnSetCurrentPath?.Invoke("", "");
        }

        public string CurrentPath {
            get {
                this.OnGetCurrentPath?.Invoke();
                return GetRemotePath();
            }
            set {                                 
                this.OnSetCurrentPath?.Invoke(GetCurrentPath(), value);
                SetRemotePath( value );
            }
        }

        [DebuggerStepThrough] public static byte[] Encoder(string text) { return Encoding.Unicode.GetBytes( text ); }

        [DebuggerStepThrough] public static string Decoder(byte[] bytes) { return Encoding.Unicode.GetString( bytes ); }


        public static Thread StartServer(int PORT) {
            var t = new Thread( () => {
                var my = new LocalHandler();
                var ls = new TcpListener( IPAddress.Any, PORT );
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
            } );
            t.Start();
            return t;
        }

        [DebuggerStepThrough] private void ResetBuffer(int size = BUFFER_SIZE) { this.buffer = new byte[size]; }

        //[DebuggerStepThrough]
        public void BufferCopy(string ac, string contend) {
            ResetBuffer( ( contend.Length + CONTEND_SIZE ) * 2 );
            Encoder( ac ).CopyTo( this.buffer, 0 );
            Encoder( contend ).CopyTo( this.buffer, CONTEND_SIZE * 2 );
        }

        [DebuggerStepThrough] public void Send() { this._cl.Client.Send( this.buffer ); }

        [DebuggerStepThrough]
        private bool ReseveBool() {
            ResetBuffer();

            var enu = new byte[this._cl.Client.Receive( this.buffer, SocketFlags.None )];
            Array.Copy( this.buffer, enu, enu.Length );

            if ( enu.Length != CONTEND_SIZE ) throw new NotSupportedException();

            switch (Decoder( enu )) {
                case TRUE:  return true;
                case FALSE: return false;
                default:    throw new ArgumentOutOfRangeException();
            }
        }

        //[DebuggerStepThrough]
        private string ReseveString() {
            ResetBuffer();

            var enu = new byte[this._cl.Client.Receive( this.buffer, SocketFlags.None )];
            Array.Copy( this.buffer, enu, enu.Length );
            return Decoder( enu );
        }

        #region Implementation of IHandler

        /// <inheritdoc />
        public string GetCurrentPath() {
            this.OnGetCurrentPath?.Invoke();
            return this.CurrentPath;
        }


        /// <inheritdoc />
        public void SetCurrentPath(string path) {       
            this.OnSetCurrentPath?.Invoke(GetCurrentPath(), path);
            this.CurrentPath = path;
        }

        /// <inheritdoc />
        public string GetRemotePath() {
            this.OnGetRemotePath?.Invoke();
            BufferCopy( GET_REMOTE_PATH, "" );
            Send();
            return ReseveString();
        }

        /// <inheritdoc />
        public void SetRemotePath(string path) {
            this.OnSetRemotePath?.Invoke();
            BufferCopy( SET_REMOTE_PATH, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public bool DirectoryExists(string path) {
            this.OnDirectoryExists?.Invoke();
            BufferCopy( DIRECTORY_EXISTS, path );
            Send();
            var str = ReseveString();

            switch (str) {
                case TRUE:  return true;
                case FALSE: return false;
                // default:    throw new ExternalException( str );
            }

            return false;
        }

        /// <inheritdoc />
        public void CreateDirectory(string path) {
            this.OnCreateDirectory?.Invoke();
            BufferCopy( CREATE_DIRECTORY, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public void CreateFile(string path) {
            this.OnCreateFile?.Invoke();
            BufferCopy( CREATE_FILE, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public void DeleteDirectory(string path) {
            this.OnDeleteDirectory?.Invoke();
            BufferCopy( DELETE_DIRECTORY, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public void DeleteFile(string path) {
            this.OnDeleteFile?.Invoke();
            BufferCopy( DELETE_FILE, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public void ValidatePath() {
            this.OnValidatePath?.Invoke();
            BufferCopy( MAKE_PATH, "" );
            Send();
            ReseveString();
        }

        /// <inheritdoc />
        public void DownloadFile(string remotePath, string localPath) {
            this.OnDownloadFile?.Invoke();
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void OpenFile(string localPath) {
            this.OnOpenFile?.Invoke();
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public string[] ListDirectory(string dirToList) {
            this.OnListDirectory?.Invoke();
            BufferCopy( GET_DIRECTORYS, dirToList );
            Send();
            var str = ReseveString();

            try {
                return From_XML( str );
            } catch (Exception e) {
                //if ( !e.Message.Contains( "(1, 1)" ) )
                //MessageBox.Show( /*e.Message +"\n" +*/ str );
                throw new Exception( str );
            }

            return null;
        }

        /// <inheritdoc />
        public string[] ListFiles(string dirToList) {
            this.OnListFiles?.Invoke();
            BufferCopy( GET_FILES, dirToList );
            Send();
            var str = ReseveString();

            try {
                return From_XML( str );
            } catch (Exception e) {
                //if ( !e.Message.Contains( "(1, 1)" ) )
                //MessageBox.Show( /*e.Message +"\n" + */str );
                throw new Exception( str );
            }

            return null;
        }

        /// <inheritdoc />
        public event Action OnGetCurrentPath;

        /// <inheritdoc />
        public event Action<string, string> OnSetCurrentPath;

        /// <inheritdoc />
        public event Action OnSetRemotePath;

        /// <inheritdoc />
        public event Action OnGetRemotePath;

        /// <inheritdoc />
        public event Action OnDirectoryExists;

        /// <inheritdoc />
        public event Action OnCreateDirectory;

        /// <inheritdoc />
        public event Action OnCreateFile;

        /// <inheritdoc />
        public event Action OnDeleteDirectory;

        /// <inheritdoc />
        public event Action OnDeleteFile;

        /// <inheritdoc />
        public event Action OnValidatePath;

        /// <inheritdoc />
        public event Action OnDownloadFile;

        /// <inheritdoc />
        public event Action OnOpenFile;

        /// <inheritdoc />
        public event Action OnListDirectory;

        /// <inheritdoc />
        public event Action OnListFiles;

        #endregion

        #region Xml

        public static string[] From_XML(string xml) {
            var serializer = new XmlSerializer( typeof(string[]) );

            using ( var reader = new StringReader( xml ) ) {
                return (string[]) serializer.Deserialize( reader );
            }
        }

        public static string To_XML(string[] contend) {
            var x = new XmlSerializer( typeof(string[]) );

            using ( var textWriter = new StringWriter() ) {
                x.Serialize( textWriter, contend );
                return textWriter.ToString();
            }
        }

        #endregion

    }
}
