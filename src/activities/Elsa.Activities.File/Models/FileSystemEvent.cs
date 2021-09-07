using System;
using System.IO;

namespace Elsa.Activities.File.Models
{
    public class FileSystemEvent
    {
        public WatcherChangeTypes ChangeType { get; set; }

        public string Directory { get; set; } = default!;

        public string FileName { get; set; } = default!;

        public string FullPath { get; set; } = default!;

        public string OldFileName { get; set; } = default!;

        public string OldFullPath { get; set; } = default!;

        public DateTime TimeStamp { get; set; } = default!;
    }
}