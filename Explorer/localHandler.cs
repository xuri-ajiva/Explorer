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
        public string GetCurrentPath() => this.CurrentPath;

        /// <inheritdoc />
        public string SetCurrentPath(string path) => this.CurrentPath = path;

        /// <inheritdoc />
        public bool DirectoryExists(string path) => Directory.Exists( path );

        /// <inheritdoc />
        public void CreateDirectory(string path, string directoryName) { Directory.CreateDirectory( path + "\\" + directoryName ); }

        /// <inheritdoc />
        public void CreateFile(string path, string filename) { File.Create( path + filename ).Close(); }

        /// <inheritdoc />
        public void DeleteDirectory(string path, string directoryName) { Directory.Delete( path + directoryName ); }

        /// <inheritdoc />
        public void DeleteFile(string path, string filename) { File.Delete( path + filename ); }

        /// <inheritdoc />
        public void ValidatePath() { this.CurrentPath = Path.GetFullPath( this.CurrentPath ); }

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
        public void SetRemotePath(string path) { this.CurrentPath = path; }

        /// <inheritdoc />
        public void DeleteFile(string filePath) { File.Delete( filePath ); }

        /// <inheritdoc />
        public string[] ListDirectory(string dirToList) => Directory.GetDirectories( dirToList );

        /// <inheritdoc />
        public string[] ListFiles(string dirToList) => Directory.GetFiles( dirToList );

        #endregion
    }
}