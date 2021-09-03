using AutoMapper;
using Elsa.Activities.File.Bookmarks;
using Elsa.Activities.File.Models;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
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
        private readonly IMapper _mapper;
        private readonly FileSystemWatcher _watcher;
        private readonly Scoped<IWorkflowLaunchpad> _workflowLaunchpad;

        public FileSystemWatcherWorker(string path, 
            string pattern, 
            ILogger<FileSystemWatcherWorker> logger, 
            IMapper mapper,
            Scoped<IWorkflowLaunchpad> workflowLaunchpad)
        {
            Path = path;
            Pattern = pattern;
            _logger = logger;
            _mapper = mapper;
            _workflowLaunchpad = workflowLaunchpad;
            _watcher = new FileSystemWatcher()
            {
                Path = path,
                Filter = pattern
            };
            _watcher.Created += FileCreated;
            _watcher.EnableRaisingEvents = true;
        }

        public string Path { get; private set; }

        public string Pattern { get; private set; }

        private void FileCreated(object sender, FileSystemEventArgs e)
        {
            var model = _mapper.Map<FileSystemEvent>(e);
            var bookmark = new FileCreatedBookmark(Path, Pattern);
            var launchContext = new WorkflowsQuery(nameof(WatchDirectory), bookmark);
            _workflowLaunchpad.UseService(s => s.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model)));
        }
    }
}
