#region using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;

#endregion

namespace ExplorerWpf.Handler {
    public sealed class LocalHandler : IHandler {
        private string           _currentPath;
        private ShellContextMenu _shellContextMenu;

        private List<string> history;

        private void OnOnError(Exception obj) { this.OnError?.Invoke( obj ); }

        #region Implementation of IHandler

        ~LocalHandler() { ReleaseUnmanagedResources(); }

        public LocalHandler(string currentPath = "") {
            this.history = new List<string> { this.RootPath, this.RootPath, this._currentPath };

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
        public void SetCurrentPath(string path, bool noHistory = false) { SetCurrentPathInternal( path, noHistory ); }

        private void SetCurrentPathInternal(string path, bool noHistory = false) {
            try {
                this.OnSetCurrentPath?.Invoke( this._currentPath, path );

                if ( string.IsNullOrEmpty( path ) ) {
                    this._currentPath = this.RootPath;
                    return;
                }

                while ( path.EndsWith( "\\" ) )
                    path = path.Substring( 0, path.Length - 1 );
                if ( string.Equals( path, this._currentPath, StringComparison.CurrentCultureIgnoreCase ) ) return;

                this._currentPath = path;

                if ( !noHistory ) {
                    //if ( this.HistoryHasFor ) {
                    //    this.history      = this.history.GetRange( 0, this.HistoryIndex + 1 );
                    //    this.HistoryIndex = this.history.Count;
                    //}

                    if ( this.history.Count > this.HistoryIndex + 1 )
                        this.history[this.HistoryIndex + 1] = this._currentPath;
                    else
                        this.history.Add( this._currentPath );
                    this.HistoryIndex++;
                }
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
        public void SetRemotePath(string path, bool noHistory) {
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

                var pInput = string.IsNullOrEmpty( this._currentPath ) ? SettingsHandler.ROOT_FOLDER : this._currentPath;
                while ( pInput.EndsWith( "\\" ) )
                    pInput = pInput.Substring( 0, pInput.Length - 1 );
                pInput            += "\\";
                this._currentPath =  Path.GetFullPath( pInput );
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
        public ReadOnlyCollection<string> PathHistory => this.history.AsReadOnly();

        /// <inheritdoc />
        public int HistoryIndex { get; private set; }

        /// <inheritdoc />
        public bool HistoryHasBack => this.history.Count > 0;

        /// <inheritdoc />
        public bool HistoryHasFor => this.history.Count > this.HistoryIndex;

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
        public void GoInHistoryTo(int index) {
            if ( index >= this.history.Count ) return;
            if ( index < 0 ) return;

            try {
                if ( index >= this.history.Count ) throw new ArgumentOutOfRangeException( nameof(index) );
                if ( index < 0 ) throw new ArgumentOutOfRangeException( nameof(index) );

                SetCurrentPathInternal( this.history[index], true );
                this.HistoryIndex = index;
            } catch (Exception e) {
                OnOnError( e );
            }
        }

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

        #region IDisposable

        private void ReleaseUnmanagedResources() {
            this._shellContextMenu.DestroyHandle();
            this._shellContextMenu = null;
            this._currentPath      = null;
            this.history.Clear();
            this.history = null;
        }

        /// <inheritdoc />
        public void Dispose() {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize( this );
        }

        #endregion

    }
}
