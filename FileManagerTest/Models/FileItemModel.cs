using System;

namespace FileManagerApp.Models
{
    public class FileItemModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string ContentType { get; set; }
    }
}
