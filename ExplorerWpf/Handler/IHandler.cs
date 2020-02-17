#region using

using System;
using System.Diagnostics;

#endregion

// ReSharper disable EventNeverSubscribedTo.Global

namespace ExplorerBase.Handlers {
    public interface IHandler {
        string GetCurrentPath();
        void   SetCurrentPath(string path);
        void   SetRemotePath(string  path);
        string GetRemotePath();

        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        void CreateFile(string      path);
        void DeleteDirectory(string path);
        void DeleteFile(string      path);
        void ValidatePath();
        void DownloadFile(string remotePath, string localPath);
        void OpenFile(string     localPath);


        string[] ListDirectory(string dirToList);
        string[] ListFiles(string     dirToList);

        event Action                 OnGetCurrentPath;
        event Action<string, string> OnSetCurrentPath;
        event Action                 OnSetRemotePath;
        event Action                 OnGetRemotePath;

        event Action OnDirectoryExists;
        event Action OnCreateDirectory;
        event Action OnCreateFile;
        event Action OnDeleteDirectory;
        event Action OnDeleteFile;
        event Action OnValidatePath;
        event Action OnDownloadFile;
        event Action OnOpenFile;

        event Action OnListDirectory;
        event Action OnListFiles;
    }
}
