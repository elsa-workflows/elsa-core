using AutoMapper;
using Elsa.Activities.File.Bookmarks;
using Elsa.Activities.File.Models;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.File.Services
{
    public class FileSystemWatchersStarter
    {
        private readonly ILogger<FileSystemWatchersStarter> _logger;
        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IServiceProvider _serviceProvider;
        private readonly ICollection<FileSystemWatcher> _watchers;
        private readonly Scoped<IWorkflowLaunchpad> _workflowLaunchpad;

        public FileSystemWatchersStarter(ILogger<FileSystemWatchersStarter> logger,
            IMapper mapper,
            IServiceScopeFactory scopeFactory,
            IServiceProvider serviceProvider,
            Scoped<IWorkflowLaunchpad> workflowLaunchpad)
        {
            _logger = logger;
            _mapper = mapper;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _watchers = new List<FileSystemWatcher>();
            _workflowLaunchpad = workflowLaunchpad;
        }

        public async Task CreateAndAddWatchersAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync();

            try
            {
                var activities = GetActivityInstancesAsync<WatchDirectory>(cancellationToken);
                await foreach (var a in activities)
                {
                    var changeTypes = await a.EvaluatePropertyValueAsync(x => x.ChangeTypes, cancellationToken);
                    var path = await a.EvaluatePropertyValueAsync(x => x.Path, cancellationToken);
                    var pattern = await a.EvaluatePropertyValueAsync(x => x.Pattern, cancellationToken);
                    CreateAndAddWatcher(path, pattern, changeTypes);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void CreateAndAddWatcher(string path, string pattern, WatcherChangeTypes changeTypes)
        {
            try
            {
                var watcher = new FileSystemWatcher()
                {
                    Path = path,
                    Filter = pattern,
                };
                watcher.Created += FileCreated;
                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
            finally
            {

            }
        }

        private async IAsyncEnumerable<IActivityBlueprintWrapper<WatchDirectory>> GetActivityInstancesAsync<TActivity>([EnumeratorCancellation] CancellationToken cancellationToken) where TActivity : IActivity
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
                var workflowBlueprintReflector = scope.ServiceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
                var workflows = await workflowRegistry.ListActiveAsync(cancellationToken);

                var query = from workflow in workflows
                            from activity in workflow.Activities
                            where activity.Type == nameof(WatchDirectory)
                            select workflow;

                foreach (var workflow in query)
                {
                    var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(scope.ServiceProvider, workflow, cancellationToken);

                    foreach (var activity in workflowBlueprintWrapper.Filter<WatchDirectory>())
                    {
                        yield return activity;
                    }
                }
            }
        }

        #region Watcher delegates
        private void FileCreated(object sender, FileSystemEventArgs e)
        {
            var watcher = (FileSystemWatcher)sender;
            var path = watcher.Path;
            var pattern = watcher.Filter;

            var model = _mapper.Map<FileSystemEvent>(e);
            var bookmark = new FileCreatedBookmark(path, pattern);
            var launchContext = new WorkflowsQuery(nameof(WatchDirectory), bookmark);
            _workflowLaunchpad.UseService(s => s.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model)));
        }
        #endregion
    }
}
