using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.MultiTenancy;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Services.Triggers
{
    public class TriggerIndexer : ITriggerIndexer
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantStore _tenantStore;

        public TriggerIndexer(
            IMediator mediator,
            ILogger<TriggerIndexer> logger,
            IServiceScopeFactory scopeFactory,
            ITenantStore tenantStore)
        {
            _mediator = mediator;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _tenantStore = tenantStore;
        }

        public async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                await IndexTriggersInternalAsync(scope.ServiceProvider, cancellationToken);
            }

            await _mediator.Publish(new TriggerIndexingFinished(), cancellationToken);
        }

        private async Task IndexTriggersInternalAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var workflowRegistry = serviceProvider.GetRequiredService<IWorkflowRegistry>();

            var allWorkflowBlueprints = await workflowRegistry.ListActiveAsync(cancellationToken);
            var publishedWorkflowBlueprints = allWorkflowBlueprints.Where(x => x.IsPublished && !x.IsDisabled).ToList();

            await IndexTriggersInternalAsync(publishedWorkflowBlueprints, serviceProvider, cancellationToken);
        }

        private async Task IndexTriggersInternalAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var triggersForBookmarksProvider = serviceProvider.GetRequiredService<IGetsTriggersForWorkflowBlueprints>();
            var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();

            _stopwatch.Restart();
            _logger.LogInformation("Indexing triggers");

            var workflowBlueprintList = workflowBlueprints.ToList();
            var triggers = (await triggersForBookmarksProvider.GetTriggersAsync(workflowBlueprintList, cancellationToken)).ToList();

            _stopwatch.Stop();
            _logger.LogInformation("Indexed {TriggerCount} triggers in {ElapsedTime}", triggers.Count, _stopwatch.Elapsed);
              
            await triggerStore.StoreAsync(triggers, cancellationToken);
        }
    }
}