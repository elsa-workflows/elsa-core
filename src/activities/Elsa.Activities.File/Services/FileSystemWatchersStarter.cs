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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IServiceProvider _serviceProvider;
        private readonly ICollection<FileSystemWatcherWorker> _workers;

        public FileSystemWatchersStarter(ILogger<FileSystemWatchersStarter> logger,
            IServiceScopeFactory scopeFactory,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _workers = new List<FileSystemWatcherWorker>();
        }

        public async Task CreateWatchersAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync();

            try
            {
                var activities = GetActivityInstancesAsync<WatchDirectory>(cancellationToken);
                await foreach (var a in activities)
                {
                    var path = await a.EvaluatePropertyValueAsync(x => x.Path, cancellationToken);
                    var pattern = await a.EvaluatePropertyValueAsync(x => x.Pattern, cancellationToken);
                    CreateAndAddWatcher(path, pattern);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void CreateAndAddWatcher(string path, string pattern)
        {
            try
            {
                var worker = ActivatorUtilities.CreateInstance<FileSystemWatcherWorker>(_serviceProvider, path, pattern);
                _workers.Add(worker);
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
    }
}
