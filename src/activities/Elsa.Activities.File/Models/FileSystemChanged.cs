using System;
using System.IO;

namespace Elsa.Activities.File.Models
{
    public class FileSystemChanged
    {
        public FileSystemChanged()
        {
        }

        public FileSystemChanged(WatcherChangeTypes changeType, string directory, string fileName, string fullPath)
        {
            ChangeType = changeType;
            Directory = directory;
            FileName = fileName;
            FullPath = fullPath;
            TimeStamp = DateTime.Now;
        }

        public WatcherChangeTypes ChangeType { get; set; }

        public string Directory { get; set; } = default!;

        public string FileName { get; set; } = default!;

        public string FullPath { get; set; } = default!;

        public DateTime TimeStamp { get; set; } = default!;
    }
}