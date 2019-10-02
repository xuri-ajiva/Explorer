using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer {
    public interface IHandler {
        string GetCurrentPath();
        string SetCurrentPath(string path);

        bool DirectoryExists(string  path);
        void CreateDirectory(string  path);
        void CreateFile(string       path);
        void DeleteDirectory(string path);
        void DeleteFile(string      path);
        void ValidatePath();
        void DownloadFile(string     remotePath, string localPath);
        void OpenFile(string         localPath);
        void SetRemotePath(string    path);

        string[] ListDirectory(string dirToList);
        string[] ListFiles(string     dirToList);
    }
}