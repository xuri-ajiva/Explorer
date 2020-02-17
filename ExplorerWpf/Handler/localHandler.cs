#region using

using System;
using System.Diagnostics;
using System.IO;

#endregion

namespace ExplorerBase.Handlers {
    public class LocalHandler : IHandler {

        #region Implementation of IHandler

        public string CurrentPath;

        public LocalHandler(string currentPath = "") {
            this.OnSetCurrentPath?.Invoke( "", currentPath );
            this.CurrentPath = currentPath;
        }

        /// <inheritdoc />
        public string GetCurrentPath() {
            this.OnGetCurrentPath?.Invoke();
            return string.IsNullOrEmpty( this.CurrentPath ) ? "/" : this.CurrentPath;
        }

        /// <inheritdoc />
        public void SetCurrentPath(string path) {
            this.OnSetCurrentPath?.Invoke( GetCurrentPath(), path );
            this.CurrentPath = path;
        }

        /// <inheritdoc />
        public string GetRemotePath() {
            this.OnGetRemotePath?.Invoke();
            return this.CurrentPath;
        }

        /// <inheritdoc />
        public void SetRemotePath(string path) {
            this.CurrentPath = path;
            this.OnSetRemotePath?.Invoke();
        }

        /// <inheritdoc />
        public bool DirectoryExists(string path) {
            this.OnDirectoryExists?.Invoke();
            return Directory.Exists( path );
        }

        /// <inheritdoc />
        public void CreateDirectory(string path) {
            this.OnCreateDirectory?.Invoke();
            Directory.CreateDirectory( path );
        }

        /// <inheritdoc />
        public void CreateFile(string path) {
            this.OnCreateFile?.Invoke();
            File.Create( path ).Close();
        }

        /// <inheritdoc />
        public void DeleteDirectory(string path) {
            this.OnDeleteDirectory?.Invoke();
            Directory.Delete( path );
        }

        /// <inheritdoc />
        public void DeleteFile(string path) {
            this.OnDeleteFile?.Invoke();
            File.Delete( path );
        }

        /// <inheritdoc />
        public void ValidatePath() {
            this.OnValidatePath?.Invoke();
            this.CurrentPath = Path.GetFullPath( string.IsNullOrEmpty( this.CurrentPath ) ? "." : this.CurrentPath );
        }

        /// <inheritdoc />
        public void DownloadFile(string remotePath, string localPath) { this.OnDownloadFile?.Invoke(); }

        /// <inheritdoc />
        public void OpenFile(string localPath) {
            this.OnOpenFile?.Invoke();

            try {
                Process.Start( localPath );
            } catch (Exception e) {
                Console.WriteLine( e.Message );

                //MessageBox.Show( e.Message );
            }
        }

        /// <inheritdoc />
        public string[] ListDirectory(string dirToList) {
            this.OnListDirectory?.Invoke();
            return Directory.GetDirectories( dirToList );
        }

        /// <inheritdoc />
        public string[] ListFiles(string dirToList) {
            this.OnListFiles?.Invoke();
            return Directory.GetFiles( dirToList );
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

    }
}
