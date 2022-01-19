using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Triggers;
using Elsa.Providers.Workflows;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Rebus.Extensions;

namespace Elsa.Services.Triggers
{
    public class TriggerIndexer : ITriggerIndexer
    {
        private readonly IProgrammaticWorkflowProvider _programmaticWorkflowProvider;
        private readonly ITriggerStore _triggerStore;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();
        private readonly IGetsTriggersForWorkflowBlueprints _triggersForBookmarksProvider;

        public TriggerIndexer(
            IProgrammaticWorkflowProvider programmaticWorkflowProvider,
            ITriggerStore triggerStore,
            IMediator mediator,
            ILogger<TriggerIndexer> logger,
            IGetsTriggersForWorkflowBlueprints triggersForBookmarksProvider)
        {
            _programmaticWorkflowProvider = programmaticWorkflowProvider;
            _triggerStore = triggerStore;
            _mediator = mediator;
            _logger = logger;
            _triggersForBookmarksProvider = triggersForBookmarksProvider;
        }

        public async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
        {
            await _triggerStore.DeleteManyAsync(new TriggerModelTypeSpecification(typeof(ProgrammaticWorkflowProvider).ToString()), cancellationToken);
            var programmaticWorkflowsBlueprints = await _programmaticWorkflowProvider.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);
            var publishedWorkflowBlueprints = programmaticWorkflowsBlueprints.Where(x => x.IsPublished && !x.IsDisabled).ToList();
            await IndexTriggersAsync(publishedWorkflowBlueprints, cancellationToken);
            await _mediator.Publish(new TriggerIndexingFinished(), cancellationToken);
        }

        public Task IndexTriggersAsync(IEnumerable<WorkflowDefinition> workflowDefinitions, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task IndexTriggersAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public async Task DeleteTriggersAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowDefinitionIdsSpecification(workflowDefinitionIds);
            await _triggerStore.DeleteManyAsync(specification, cancellationToken);
        }

        public async Task DeleteTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowDefinitionIdSpecification(workflowDefinitionId);
            var count = await _triggerStore.DeleteManyAsync(specification, cancellationToken);
            _logger.LogDebug("Deleted {DeletedTriggerCount} triggers for workflow {WorkflowDefinitionId}", count, workflowDefinitionId);
        }

        private async Task IndexTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints, CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            _logger.LogInformation("Indexing triggers");

            var workflowBlueprintList = workflowBlueprints.ToList();
            var triggers = (await _triggersForBookmarksProvider.GetTriggersAsync(workflowBlueprintList, cancellationToken)).ToList();

            _stopwatch.Stop();
            _logger.LogInformation("Indexed {TriggerCount} triggers in {ElapsedTime}", triggers.Count, _stopwatch.Elapsed);

            foreach (var trigger in triggers)
            {
                await _triggerStore.SaveAsync(new Trigger()
                {
                    ActivityId = trigger.ActivityId,
                    ActivityType = trigger.ActivityType,
                    Hash = trigger.BookmarkHash,
                    TenantId = trigger.WorkflowBlueprint.TenantId,
                    WorkflowDefinitionId = trigger.WorkflowBlueprint.Id,
                    Model = trigger.GetType().GetSimpleAssemblyQualifiedName()
                }, cancellationToken);
            }
        }
    }
}