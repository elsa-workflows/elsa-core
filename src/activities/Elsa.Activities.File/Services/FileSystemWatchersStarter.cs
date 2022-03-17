using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Activities.File.Bookmarks;
using Elsa.Activities.File.Models;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.File.Services
{
    public class FileSystemWatchersStarter
    {
        private readonly ILogger<FileSystemWatchersStarter> _logger;
        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly ICollection<FileSystemWatcher> _watchers;
        private readonly IBookmarkSerializer _bookmarkSerializer;

        public FileSystemWatchersStarter(
            ILogger<FileSystemWatchersStarter> logger,
            IMapper mapper,
            IServiceScopeFactory scopeFactory,
            IBookmarkSerializer bookmarkSerializer)
        {
            _logger = logger;
            _mapper = mapper;
            _scopeFactory = scopeFactory;
            _bookmarkSerializer = bookmarkSerializer;
            _watchers = new List<FileSystemWatcher>();
        }

        public async Task CreateAndAddWatchersAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                DisposeExistingWatchers();

                var triggerFinder = serviceProvider.GetRequiredService<ITriggerFinder>();
                await triggerFinder.FindTriggersAsync<WatchDirectory>(cancellationToken: cancellationToken);

                var triggers = await triggerFinder.FindTriggersByTypeAsync<FileSystemEventBookmark>(cancellationToken: cancellationToken);

                foreach (var trigger in triggers)
                {
                    var bookmark = _bookmarkSerializer.Deserialize<FileSystemEventBookmark>(trigger.Model);

                    var changeTypes = bookmark.ChangeTypes;
                    var notifyFilters = bookmark.NotifyFilters;
                    var path = bookmark.Path;
                    var pattern = bookmark.Pattern;
                    CreateAndAddWatcher(path, pattern, changeTypes, notifyFilters, serviceProvider);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void DisposeExistingWatchers()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Dispose();
                _watchers.Remove(watcher);
            }
        }

        private void CreateAndAddWatcher(string? path, string? pattern, WatcherChangeTypes changeTypes, NotifyFilters notifyFilters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File watcher path must not be null or empty");

            EnsurePathExists(path);

            var watcher = new FileSystemWatcher()
            {
                Path = path,
                Filter = pattern,
                NotifyFilter = notifyFilters
            };

            if (changeTypes == WatcherChangeTypes.Created || changeTypes == WatcherChangeTypes.All)
                watcher.Created += (object sender, FileSystemEventArgs e) =>
                {
                    StartWorkflow((FileSystemWatcher)sender, e, serviceProvider);
                };

            if (changeTypes == WatcherChangeTypes.Changed || changeTypes == WatcherChangeTypes.All)
                watcher.Changed += (object sender, FileSystemEventArgs e) =>
                {
                    if (e.ChangeType != WatcherChangeTypes.Changed) return;

                    StartWorkflow((FileSystemWatcher)sender, e, serviceProvider);
                };

            if (changeTypes == WatcherChangeTypes.Deleted || changeTypes == WatcherChangeTypes.All)
                watcher.Deleted += (object sender, FileSystemEventArgs e) =>
                {
                    StartWorkflow((FileSystemWatcher)sender, e, serviceProvider);
                };

            if (changeTypes == WatcherChangeTypes.Renamed || changeTypes == WatcherChangeTypes.All)
                watcher.Renamed += (object sender, RenamedEventArgs e) =>
                {
                    StartWorkflow((FileSystemWatcher)sender, e, serviceProvider);
                };

            watcher.EnableRaisingEvents = true;

            _watchers.Add(watcher);
        }

        private void EnsurePathExists(string path)
        {
            _logger.LogDebug("Checking ${Path} exists", path);

            if (Directory.Exists(path))
                return;

            _logger.LogInformation("Creating directory {Path}", path);
            Directory.CreateDirectory(path);
        }

        private async void StartWorkflow(FileSystemWatcher watcher, FileSystemEventArgs e, IServiceProvider serviceProvider)
        {
            var changeTypes = e.ChangeType;
            var notifyFilter = watcher.NotifyFilter;
            var path = watcher.Path;
            var pattern = watcher.Filter;

            var model = _mapper.Map<FileSystemEvent>(e);
            var bookmark = new FileSystemEventBookmark(path, pattern, changeTypes, notifyFilter);
            var launchContext = new WorkflowsQuery(nameof(WatchDirectory), bookmark);

            var workflowLaunchpad = serviceProvider.GetRequiredService<IWorkflowLaunchpad>();
            await workflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model));
        }
    }
}