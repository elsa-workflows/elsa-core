using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Services.Triggers
{
    public class TriggerIndexer : ITriggerIndexer
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly ITriggerStore _triggerStore;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();
        private readonly IGetsTriggersForWorkflowBlueprints _triggersForBookmarksProvider;

        public TriggerIndexer(
            IWorkflowRegistry workflowRegistry,
            ITriggerStore triggerStore,
            IMediator mediator,
            ILogger<TriggerIndexer> logger,
            IGetsTriggersForWorkflowBlueprints triggersForBookmarksProvider)
        {
            _workflowRegistry = workflowRegistry;
            _triggerStore = triggerStore;
            _mediator = mediator;
            _logger = logger;
            _triggersForBookmarksProvider = triggersForBookmarksProvider;
        }

        public async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
        {
            var allWorkflowBlueprints = await _workflowRegistry.ListActiveAsync(cancellationToken);
            var publishedWorkflowBlueprints = allWorkflowBlueprints.Where(x => x.IsPublished).ToList();
            await IndexTriggersAsync(publishedWorkflowBlueprints, cancellationToken);
            await _mediator.Publish(new TriggerIndexingFinished(), cancellationToken);
        }

        private async Task IndexTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints, CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            _logger.LogInformation("Indexing triggers");

            var workflowBlueprintList = workflowBlueprints.ToList();
            var triggers = (await _triggersForBookmarksProvider.GetTriggersAsync(workflowBlueprintList, cancellationToken)).ToList();

            _stopwatch.Stop();
            _logger.LogInformation("Indexed {TriggerCount} triggers in {ElapsedTime}", triggers.Count, _stopwatch.Elapsed);

            await _triggerStore.StoreAsync(triggers, cancellationToken);
        }
    }
}