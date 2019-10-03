using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Explorer {
    internal class LocalHandler : IHandler {
        #region Implementation of IHandler

        public string CurrentPath;
        public LocalHandler(string currentPath = "") { this.CurrentPath = currentPath; }

        /// <inheritdoc />
        public string GetCurrentPath() => string.IsNullOrEmpty( this.CurrentPath ) ? "/" : this.CurrentPath;

        /// <inheritdoc />
        public void SetCurrentPath(string path) => this.CurrentPath = path;

        /// <inheritdoc />
        public string GetRemotePath() => this.CurrentPath;

        /// <inheritdoc />
        public void SetRemotePath(string path) { this.CurrentPath = path; }

        /// <inheritdoc />
        public bool DirectoryExists(string path) => Directory.Exists( path );

        /// <inheritdoc />
        public void CreateDirectory(string path) { Directory.CreateDirectory( path ); }

        /// <inheritdoc />
        public void CreateFile(string path) { File.Create( path ).Close(); }

        /// <inheritdoc />
        public void DeleteDirectory(string path) { Directory.Delete( path ); }

        /// <inheritdoc />
        public void DeleteFile(string path) { File.Delete( path ); }

        /// <inheritdoc />
        public void ValidatePath() { this.CurrentPath = Path.GetFullPath( string.IsNullOrEmpty( this.CurrentPath ) ? "." : this.CurrentPath ); }

        /// <inheritdoc />
        public void DownloadFile(string remotePath, string localPath) { }

        /// <inheritdoc />
        public void OpenFile(string localPath) {
            try {
                Process.Start( localPath );
            } catch (Exception e) {
                MessageBox.Show( e.Message );
            }
        }

        /// <inheritdoc />
        public string[] ListDirectory(string dirToList) => Directory.GetDirectories( dirToList );

        /// <inheritdoc />
        public string[] ListFiles(string dirToList) => Directory.GetFiles( dirToList );

        #endregion
    }
}