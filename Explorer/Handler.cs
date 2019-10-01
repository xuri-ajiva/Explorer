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
        void CreateDirectory(string  path, string directoryName);
        void CreateFile(string       path, string filename);
        void DeleteDirectory(string path, string directoryName);
        void DeleteFile(string      path, string filename);
        void ValidatePath();
        void DownloadFile(string     remotePath, string localPath);
        void OpenFile(string         localPath);
        void SetRemotePath(string    path);
        void DeleteFile(string       filePath);

        string[] ListDirectory(string dirToList);
        string[] ListFiles(string     dirToList);
    }
}