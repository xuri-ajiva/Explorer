using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer {
    public interface IHandler {
        bool DirectoryExists(string  path);
        void CreateDirectory(string  path, string directoryName);
        void CreateFile(string       path, string filename);
        void AddSpecialDirs(ref int  count);
        void ListDirectory(string    dirToList, ref int count);
        void ListFiles(string        dirToList, ref int count);
        void ValidatePath(ref string path);
        void DownloadFile(string     remotePath, string localPath);
        void SetRemotePath(string path);
        void DeleteFile(string filePath);
    }
}