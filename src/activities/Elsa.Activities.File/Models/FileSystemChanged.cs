using System;
using System.IO;

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
            TimeStamp = DateTime.Now;
        }

        public WatcherChangeTypes ChangeType { get; set; }

        public string Directory { get; set; }

        public string FileName { get; set; }

        public string FullPath { get; set; }

        public string Pattern { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}
