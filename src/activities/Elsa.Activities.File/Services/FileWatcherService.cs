using Elsa.Activities.File.Extensions;
using Elsa.Activities.File.Options;
using Elsa.Dispatch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.File.Services
{
    public class FileWatcherService : BackgroundService
    {
        private FileSystemWatcher _watcher;
        private ILogger _logger;
        private IOptions<FileWatcherOptions> _options;
        private IWorkflowDispatcher _workflowDispatcher;

        public FileWatcherService(ILogger<FileWatcherService> logger, IOptions<FileWatcherOptions> options, IWorkflowDispatcher workflowRunner)
        {
            _logger = logger;
            _options = options;
            _workflowDispatcher = workflowRunner;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service");
            if (!Directory.Exists(_options.Value.Path))
            {
                _logger.LogError($"Folder to watch does not exist: {_options.Value.Path}");
                return Task.CompletedTask;
            }

            _watcher = new FileSystemWatcher()
            {
                NotifyFilter = NotifyFilters.FileName 
                    | NotifyFilters.DirectoryName,
                Filter = _options.Value.Pattern,
                Path = _options.Value.Path,
                IncludeSubdirectories = _options.Value.SubDirectories
            };
            _watcher.Created += Watcher_Created;
            _watcher.EnableRaisingEvents = true;

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping service");
            _watcher.EnableRaisingEvents = false;
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _watcher.Dispose();
            base.Dispose();
        }

        private async void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"File created: {e.FullPath}-{e.ChangeType}");
            await _workflowDispatcher.TriggerFileSystemChangedWorkflowsAsync(_watcher.Path, e.ChangeType, e.Name, e.FullPath);
        }
    }
}
