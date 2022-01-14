using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Services.Triggers
{
    public class TriggerIndexer : ITriggerIndexer
    { 
        protected readonly IMediator _mediator;
        protected readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();

        public TriggerIndexer(
            IMediator mediator,
            ILogger<TriggerIndexer> logger,
            IServiceScopeFactory scopeFactory)
        {
            _mediator = mediator;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public virtual async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();

            await IndexTriggersInternalAsync(scope.ServiceProvider, cancellationToken);            

            await _mediator.Publish(new TriggerIndexingFinished(), cancellationToken);
        }

        protected async Task IndexTriggersInternalAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
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