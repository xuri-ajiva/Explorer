namespace ExplorerBase {
    internal class NullHandler : IHandler {

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

        #endregion

    }
}
