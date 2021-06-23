using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Elsa.Activities.File.Services
{
    public class FileSystemWatcherFactory
    {
        private readonly IDictionary<string, FileSystemWatcher> _watchers = new Dictionary<string, FileSystemWatcher>();
    }
}
