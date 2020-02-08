#region using

using System;
using System.Diagnostics;
using System.IO;

#endregion

namespace ExplorerBase {
    public class LocalHandler : IHandler {

        #region Implementation of IHandler

        public string CurrentPath;

        public LocalHandler(string currentPath = "") {
            this.CurrentPath = currentPath;
            OnSetCurrentPath?.Invoke();
        }

        /// <inheritdoc />
        public string GetCurrentPath() {
            OnGetCurrentPath?.Invoke();
            return string.IsNullOrEmpty( this.CurrentPath ) ? "/" : this.CurrentPath;
        }

        /// <inheritdoc />
        public void SetCurrentPath(string path) {
            this.CurrentPath = path;
            OnSetCurrentPath?.Invoke();
        }

        /// <inheritdoc />
        public string GetRemotePath() {
            OnGetRemotePath?.Invoke();
            return this.CurrentPath;
        }

        /// <inheritdoc />
        public void SetRemotePath(string path) {
            this.CurrentPath = path;
            OnSetRemotePath?.Invoke();
        }

        /// <inheritdoc />
        public bool DirectoryExists(string path) {
            OnDirectoryExists?.Invoke();
            return Directory.Exists( path );
        }

        /// <inheritdoc />
        public void CreateDirectory(string path) {
            OnCreateDirectory?.Invoke();
            Directory.CreateDirectory( path );
        }

        /// <inheritdoc />
        public void CreateFile(string path) {
            OnCreateFile?.Invoke();
            File.Create( path ).Close();
        }

        /// <inheritdoc />
        public void DeleteDirectory(string path) {
            OnDeleteDirectory?.Invoke();
            Directory.Delete( path );
        }

        /// <inheritdoc />
        public void DeleteFile(string path) {
            OnDeleteFile?.Invoke();
            File.Delete( path );
        }

        /// <inheritdoc />
        public void ValidatePath() {
            OnValidatePath?.Invoke();
            this.CurrentPath = Path.GetFullPath( string.IsNullOrEmpty( this.CurrentPath ) ? "." : this.CurrentPath );
        }

        /// <inheritdoc />
        public void DownloadFile(string remotePath, string localPath) { OnDownloadFile?.Invoke(); }

        /// <inheritdoc />
        public void OpenFile(string localPath) {
            OnOpenFile?.Invoke();

            try {
                Process.Start( localPath );
            } catch (Exception e) {
                Console.WriteLine( e.Message );
                //MessageBox.Show( e.Message );
            }
        }

        /// <inheritdoc />
        public string[] ListDirectory(string dirToList) {
            OnListDirectory?.Invoke();
            return Directory.GetDirectories( dirToList );
        }

        /// <inheritdoc />
        public string[] ListFiles(string dirToList) {
            OnListFiles?.Invoke();
            return Directory.GetFiles( dirToList );
        }

        /// <inheritdoc />
        public event Action OnGetCurrentPath;

        /// <inheritdoc />
        public event Action OnSetCurrentPath;

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
