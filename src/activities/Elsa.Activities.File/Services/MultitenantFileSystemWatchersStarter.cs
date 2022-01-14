using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.MultiTenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.File.Services
{
    public class MultitenantFileSystemWatchersStarter : FileSystemWatchersStarter
    {
        private readonly ITenantStore _tenantStore;

        public MultitenantFileSystemWatchersStarter(
            ILogger<MultitenantFileSystemWatchersStarter> logger,
            IMapper mapper,
            IServiceScopeFactory scopeFactory,
            Scoped<IWorkflowLaunchpad> workflowLaunchpad,
            ITenantStore tenantStore) : base (logger, mapper, scopeFactory, workflowLaunchpad)
        {
            _tenantStore = tenantStore;
        }

        public override async Task CreateAndAddWatchersAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_watchers.Any())
                {
                    foreach (var (watcher, _) in _watchers)
                        watcher.Dispose();

                    _watchers.Clear();
                }

                foreach (var tenant in _tenantStore.GetTenants())
                {
                    using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                    var activities = GetActivityInstancesAsync(scope.ServiceProvider, cancellationToken);
                    await foreach (var a in activities.WithCancellation(cancellationToken))
                    {
                        var changeTypes = await a.EvaluatePropertyValueAsync(x => x.ChangeTypes, cancellationToken);
                        var notifyFilters = await a.EvaluatePropertyValueAsync(x => x.NotifyFilters, cancellationToken);
                        var path = await a.EvaluatePropertyValueAsync(x => x.Path, cancellationToken);
                        var pattern = await a.EvaluatePropertyValueAsync(x => x.Pattern, cancellationToken);
                        CreateAndAddWatcher(path, pattern, changeTypes, notifyFilters, tenant);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
