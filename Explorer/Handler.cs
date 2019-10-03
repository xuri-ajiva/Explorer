using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer {
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
    }
}