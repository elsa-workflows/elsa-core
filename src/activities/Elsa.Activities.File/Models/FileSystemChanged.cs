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

        public WatcherChangeTypes ChangeType { get; set; }

        public string Directory { get; set; }

        public string FileName { get; set; }
    }
}
