#region using

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

#endregion

namespace Explorer {
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

        public string CurrentPath;

        public RemoteHandler(TcpClient cl) {
            this._cl         = cl;
            this.CurrentPath = "";
        }

        [DebuggerStepThrough] private void ResetBuffer(int size = BUFFER_SIZE) { this.buffer = new byte[size]; }

        [DebuggerStepThrough] public static byte[] Encoder(string text)  => Encoding.Unicode.GetBytes( text );
        [DebuggerStepThrough] public static string Decoder(byte[] bytes) => Encoding.Unicode.GetString( bytes );

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

        [DebuggerStepThrough]
        private string ReseveString() {
            ResetBuffer();

            var enu = new byte[this._cl.Client.Receive( this.buffer, SocketFlags.None )];
            Array.Copy( this.buffer, enu, enu.Length );
            return Decoder( enu );
        }


        #region Implementation of IHandler

        /// <inheritdoc />
        public string GetCurrentPath() => this.CurrentPath;

        /// <inheritdoc />
        public string SetCurrentPath(string path) => this.CurrentPath = path;

        /// <inheritdoc />
        public bool DirectoryExists(string path) {
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
            BufferCopy( CREATE_DIRECTORY, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public void CreateFile(string path) {
            BufferCopy( CREATE_FILE, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public void DeleteDirectory(string path) {
            BufferCopy( DELETE_DIRECTORY, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public void DeleteFile(string path) {
            BufferCopy( DELETE_FILE, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public void ValidatePath() {
            BufferCopy( MAKE_PATH, "" );
            Send();
            var str = ReseveString();
            //if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public void DownloadFile(string remotePath, string localPath) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public void OpenFile(string localPath) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public void SetRemotePath(string path) {
            BufferCopy( SET_REMOTE_PATH, path );
            Send();
            var str = ReseveString();
            if ( str != TRUE ) throw new ExternalException( str );
        }

        /// <inheritdoc />
        public string[] ListDirectory(string dirToList) {
            BufferCopy( GET_DIRECTORYS, dirToList );
            Send();
            var str = ReseveString();
            try {
                return From_XML( str );
            } catch (Exception e) {
                if ( !e.Message.Contains( "(1, 1)" ) )
                    MessageBox.Show( e.Message +"\n" + str);
            }

            return null;
        }

        /// <inheritdoc />
        public string[] ListFiles(string dirToList) {
            BufferCopy( GET_FILES, dirToList );
            Send();
            var str = ReseveString();
            try {
                return From_XML( str );
            } catch (Exception e) {
                if ( !e.Message.Contains( "(1, 1)" ) )
                    MessageBox.Show( e.Message +"\n" + str);
            }

            return null;
        }

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
            using ( StringWriter textWriter = new StringWriter() ) {
                x.Serialize( textWriter, contend );
                return textWriter.ToString();
            }
        }

        #endregion
    }
}