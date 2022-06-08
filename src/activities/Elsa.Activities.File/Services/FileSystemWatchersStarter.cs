using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public async Task CreateAndAddWatchersAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                DisposeExistingWatchers();

                using var scope = _scopeFactory.CreateScope();
                var triggerFinder = scope.ServiceProvider.GetRequiredService<ITriggerFinder>();
                var triggerRemover = scope.ServiceProvider.GetRequiredService<ITriggerRemover>();
                await triggerFinder.FindTriggersAsync<WatchDirectory>(cancellationToken: cancellationToken);

                var triggers = await triggerFinder.FindTriggersByTypeAsync<FileSystemEventBookmark>(cancellationToken: cancellationToken);

                foreach (var trigger in triggers)
                {
                    var bookmark = _bookmarkSerializer.Deserialize<FileSystemEventBookmark>(trigger.Model);

                    var changeTypes = bookmark.ChangeTypes;
                    var notifyFilters = bookmark.NotifyFilters;
                    var path = bookmark.Path;
                    var pattern = bookmark.Pattern;
                    try
                    {
                        CreateAndAddWatcher(path, pattern, changeTypes, notifyFilters);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning(ex,
                            $"Watcher with path \"{path}\" and pattern \"{pattern}\" causes IOException. Removing Trigger.",
                            path,
                            pattern,
                            changeTypes,
                            notifyFilters);
                        await triggerRemover.RemoveTriggerAsync(trigger);
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex,
                            $"Watcher with path \"{path}\" and pattern \"{pattern}\" is not valid. Removing Trigger.",
                            path,
                            pattern,
                            changeTypes,
                            notifyFilters);
                        await triggerRemover.RemoveTriggerAsync(trigger);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void DisposeExistingWatchers()
        {
            foreach (var watcher in _watchers.ToList())
            {
                watcher.Dispose();
                _watchers.Remove(watcher);
            }
        }

        private void CreateAndAddWatcher(string? path, string? pattern, WatcherChangeTypes changeTypes, NotifyFilters notifyFilters)
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
                watcher.Created += FileCreated;

            if (changeTypes == WatcherChangeTypes.Changed || changeTypes == WatcherChangeTypes.All)
                watcher.Changed += FileChanged;

            if (changeTypes == WatcherChangeTypes.Deleted || changeTypes == WatcherChangeTypes.All)
                watcher.Deleted += FileDeleted;

            if (changeTypes == WatcherChangeTypes.Renamed || changeTypes == WatcherChangeTypes.All)
                watcher.Renamed += FileRenamed;

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

        #region Watcher delegates

        private void FileCreated(object sender, FileSystemEventArgs e)
        {
            StartWorkflow((FileSystemWatcher)sender, e);
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            StartWorkflow((FileSystemWatcher)sender, e);
        }

        private void FileDeleted(object sender, FileSystemEventArgs e)
        {
            StartWorkflow((FileSystemWatcher)sender, e);
        }

        private void FileRenamed(object sender, RenamedEventArgs e)
        {
            StartWorkflow((FileSystemWatcher)sender, e);
        }

        private async void StartWorkflow(FileSystemWatcher watcher, FileSystemEventArgs e)
        {
            var changeTypes = e.ChangeType;
            var notifyFilter = watcher.NotifyFilter;
            var path = watcher.Path;
            var pattern = watcher.Filter;

            var model = _mapper.Map<FileSystemEvent>(e);
            var bookmark = new FileSystemEventBookmark(path, pattern, changeTypes, notifyFilter);
            var launchContext = new WorkflowsQuery(nameof(WatchDirectory), bookmark);

            using var scope = _scopeFactory.CreateScope();
            var workflowLaunchpad = scope.ServiceProvider.GetRequiredService<IWorkflowLaunchpad>();
            await workflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model));
        }

        #endregion
    }
}