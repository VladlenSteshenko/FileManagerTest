using System.Collections.Generic;

namespace FileManagerApp.Models
{
    public class DirectoryContentModel
    {
        public string CurrentPath { get; set; }
        public string ParentPath { get; set; }
        public List<FileItemModel> Items { get; set; }
    }
}
