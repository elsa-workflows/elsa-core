using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Elsa.Activities.File.Services
{
    public class FileSystemWatcherWorker
    {
        private readonly ILogger _logger;
        private readonly FileSystemWatcher _watcher;

        public FileSystemWatcherWorker(string path, string pattern, ILogger<FileSystemWatcherWorker> logger)
        {
            Path = path;
            Pattern = pattern;
            _logger = logger;
            _watcher = new FileSystemWatcher()
            {
                Path = path,
                Filter = pattern
            };
            _watcher.Created += FileCreated;
            _watcher.Changed += FileChanged;
            _watcher.EnableRaisingEvents = true;
        }

        public string Path { get; private set; }

        public string Pattern { get; private set; }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
        }

        private void FileCreated(object sender, FileSystemEventArgs e)
        {
        }
    }
}
