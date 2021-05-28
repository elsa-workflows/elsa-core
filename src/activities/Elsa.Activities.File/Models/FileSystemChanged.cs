using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.File.Models
{
    public class FileSystemChanged
    {
        public FileSystemChanged()
        { }

        public FileSystemChanged(WatcherChangeTypes changeType, string directory, string fileName, string fullPath)
        {
            ChangeType = changeType;
            Directory = directory;
            FileName = fileName;
            FullPath = fullPath;
        }

        public WatcherChangeTypes ChangeType { get; set; }

        public string Directory { get; set; }

        public string FileName { get; set; }

        public string FullPath { get; set; }

        public string Pattern { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}
