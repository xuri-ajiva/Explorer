#region using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;

#endregion

// ReSharper disable EventNeverSubscribedTo.Global

namespace ExplorerWpf.Handler {
    public interface IHandler: IDisposable {
        string GetCurrentPath();
        void   SetCurrentPath(string path, bool noHistory = false);
        void   SetRemotePath(string  path, bool noHistory = false);
        string GetRemotePath();
        bool   DirectoryExists(string path);
        void   ValidatePath();
        void   DownloadFile(string remotePath, string localPath);
        void   OpenFile(string     localPath);


        void ShowContextMenu(FileInfo[]      pathFileInfos,      Point locationPoint);
        void ShowContextMenu(DirectoryInfo[] pathDirectoryInfos, Point locationPoint);

        string RootPath { get; }
        ReadOnlyCollection<string> PathHistory { get;}
        int HistoryIndex { get; }
        bool HistoryHasBack { get; }
        bool HistoryHasFor { get; }

        void GoInHistoryTo(int index);

        DirectoryInfo[] ListDirectory(string dirToList);
        FileInfo[]      ListFiles(string     dirToList);

        event Action<Exception> OnError;

        event Action                 OnGetCurrentPath;
        event Action<string, string> OnSetCurrentPath;
        event Action                 OnSetRemotePath;
        event Action                 OnGetRemotePath;

        event Action OnDirectoryExists;
        event Action OnValidatePath;
        event Action OnDownloadFile;
        event Action OnOpenFile;

        event Action OnListDirectory;
        event Action OnListFiles;

        void ThrowError(Exception exception);
    }
}
