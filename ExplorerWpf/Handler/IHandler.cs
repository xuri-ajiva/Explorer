#region using

using System;
using System.Drawing;
using System.IO;

#endregion

// ReSharper disable EventNeverSubscribedTo.Global

namespace ExplorerWpf.Handler {
    public interface IHandler {
        string GetCurrentPath();
        void   SetCurrentPath(string path);
        void   SetRemotePath(string  path);
        string GetRemotePath();
        bool DirectoryExists(string path);
        void ValidatePath();
        void DownloadFile(string remotePath, string localPath);
        void OpenFile(string     localPath);


        void ShowContextMenu(FileInfo[] pathFileInfos, Point locationPoint);
        void ShowContextMenu(DirectoryInfo[] pathDirectoryInfos, Point locationPoint);

        string RootPath { get; }

        DirectoryInfo[] ListDirectory(string dirToList);
        FileInfo[] ListFiles(string     dirToList);

        event Action<Exception> OnError; 

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

        void ThrowError(Exception exception);
    }
}
