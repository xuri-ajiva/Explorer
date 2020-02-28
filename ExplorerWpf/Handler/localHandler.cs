#region using

using Peter;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

#endregion

namespace ExplorerWpf.Handler {
    public sealed class LocalHandler : IHandler {
        private          string           _currentPath;
        private readonly ShellContextMenu _shellContextMenu;

        #region Implementation of IHandler

        ~LocalHandler() {
            this._shellContextMenu.DestroyHandle();
            this._currentPath = null;
        }

        public LocalHandler(string currentPath = "") {
            try {
                this.OnSetCurrentPath?.Invoke( "", currentPath );
                this._currentPath      = string.IsNullOrEmpty( currentPath ) ? SettingsHandler.ROOT_FOLDER : currentPath;
                this._shellContextMenu = new ShellContextMenu();
            } catch (Exception e) {
                OnOnError( e );
            }
        }

        /// <inheritdoc />
        public string GetCurrentPath() {
            try {
                this.OnGetCurrentPath?.Invoke();
                return string.IsNullOrEmpty( this._currentPath ) ? SettingsHandler.ROOT_FOLDER : this._currentPath;
            } catch (Exception e) {
                OnOnError( e );
                return default;
            }
        }

        /// <inheritdoc />
        public void SetCurrentPath(string path) {
            try {
                this.OnSetCurrentPath?.Invoke( GetCurrentPath(), path );
                this._currentPath = path;
            } catch (Exception e) {
                OnOnError( e );
            }
        }

        /// <inheritdoc />
        public string GetRemotePath() {
            try {
                this.OnGetRemotePath?.Invoke();
                return this._currentPath;
            } catch (Exception e) {
                OnOnError( e );
                return default;
            }
        }

        /// <inheritdoc />
        public void SetRemotePath(string path) {
            try {
                this._currentPath = path;
                this.OnSetRemotePath?.Invoke();
            } catch (Exception e) {
                OnOnError( e );
            }
        }

        /// <inheritdoc />
        public bool DirectoryExists(string path) {
            try {
                this.OnDirectoryExists?.Invoke();
                return Directory.Exists( path );
            } catch (Exception e) {
                OnOnError( e );
                return default;
            }
        }

        /// <inheritdoc />
        public void ValidatePath() {
            if ( this._currentPath == SettingsHandler.ROOT_FOLDER )
                return;

            var cp = this._currentPath;

            try {
                this.OnValidatePath?.Invoke();

                this._currentPath = Path.GetFullPath( string.IsNullOrEmpty( this._currentPath ) ? SettingsHandler.ROOT_FOLDER : this._currentPath );
            } catch (Exception e) {
                OnOnError( e );
                this._currentPath = cp;
            }
        }

        /// <inheritdoc />
        public void DownloadFile(string remotePath, string localPath) {
            try {
                this.OnDownloadFile?.Invoke();
            } catch (Exception e) {
                OnOnError( e );
            }
        }

        /// <inheritdoc />
        public void OpenFile(string localPath) {
            try {
                this.OnOpenFile?.Invoke();

                Process.Start( localPath );
            } catch (Exception e) {
                OnOnError( e );
            }
        }

        /// <inheritdoc />
        public void ShowContextMenu(FileInfo[] pathFileInfos, Point locationPoint) {
            try {
                this._shellContextMenu.ShowContextMenu( pathFileInfos, locationPoint );
            } catch (Exception e) {
                OnOnError( e );
            }
        }

        /// <inheritdoc />
        public void ShowContextMenu(DirectoryInfo[] pathDirectoryInfos, Point locationPoint) {
            try {
                this._shellContextMenu.ShowContextMenu( pathDirectoryInfos, locationPoint );
            } catch (Exception e) {
                OnOnError( e );
            }
        }

        /// <inheritdoc />
        public string RootPath => SettingsHandler.ROOT_FOLDER;

        /// <inheritdoc />
        public FileInfo[] ListFiles(string dirToList) {
            try {
                this.OnListFiles?.Invoke();
                return new DirectoryInfo( dirToList ).GetFiles();
            } catch (Exception e) {
                OnOnError( e );
                return default;
            }
        }

        /// <inheritdoc />
        public event Action<Exception> OnError;

        /// <inheritdoc />
        public DirectoryInfo[] ListDirectory(string dirToList) {
            try {
                this.OnListDirectory?.Invoke();
                return new DirectoryInfo( dirToList ).GetDirectories();
            } catch (Exception e) {
                OnOnError( e );
                return default;
            }
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
        public event Action OnValidatePath;

        /// <inheritdoc />
        public event Action OnDownloadFile;

        /// <inheritdoc />
        public event Action OnOpenFile;

        /// <inheritdoc />
        public event Action OnListDirectory;

        /// <inheritdoc />
        public event Action OnListFiles;

        /// <inheritdoc />
        public void ThrowError(Exception obj) { OnOnError( obj ); }

        #endregion

        private void OnOnError(Exception obj) { this.OnError?.Invoke( obj ); }
    }
}
