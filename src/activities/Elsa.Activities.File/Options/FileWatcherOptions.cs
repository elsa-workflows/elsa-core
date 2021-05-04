using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Activities.File.Options
{
    public class FileWatcherOptions
    {
        public string FilePattern { get; set; }
        public string Path { get; set; }
        public bool SubDirectories { get; set; } = false;
    }
}
