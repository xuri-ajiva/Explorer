using System;

namespace ExplorerBase {
    public class NullHandler : IHandler {

        #region Implementation of IHandler

        /// <inheritdoc />
        public string GetCurrentPath() => null;

        /// <inheritdoc />
        public void SetCurrentPath(string path) { }

        /// <inheritdoc />
        public void SetRemotePath(string path) { }

        /// <inheritdoc />
        public string GetRemotePath() => null;

        /// <inheritdoc />
        public bool DirectoryExists(string path) => false;

        /// <inheritdoc />
        public void CreateDirectory(string path) { }

        /// <inheritdoc />
        public void CreateFile(string path) { }

        /// <inheritdoc />
        public void DeleteDirectory(string path) { }

        /// <inheritdoc />
        public void DeleteFile(string path) { }

        /// <inheritdoc />
        public void ValidatePath() { }

        /// <inheritdoc />
        public void DownloadFile(string remotePath, string localPath) { }

        /// <inheritdoc />
        public void OpenFile(string localPath) { }

        /// <inheritdoc />
        public string[] ListDirectory(string dirToList) { return new string[] { }; }

        /// <inheritdoc />
        public string[] ListFiles(string dirToList) { return new string[] { }; }

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
