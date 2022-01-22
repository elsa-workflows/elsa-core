using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Triggers;
using Elsa.Providers.Workflows;
using Elsa.Services.Models;
using Elsa.Services.Workflows;
using MediatR;
using Microsoft.Extensions.Logging;
using Rebus.Extensions;

namespace Elsa.Services.Triggers
{
    public class TriggerIndexer : ITriggerIndexer
    {
        private static Func<IWorkflowProvider, bool> SkipDynamicProviders => x => !x.GetType().GetCustomAttributes<SkipTriggerIndexingAttribute>().Any();
        private readonly ITriggerStore _triggerStore;
        private readonly IMediator _mediator;
        private readonly IEnumerable<IWorkflowProvider> _workflowProviders;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();
        private readonly IGetsTriggersForWorkflowBlueprints _getsTriggersForWorkflows;

        public TriggerIndexer(
            ITriggerStore triggerStore,
            IMediator mediator,
            IWorkflowRegistry workflowRegistry,
            IEnumerable<IWorkflowProvider> workflowProviders,
            ILogger<TriggerIndexer> logger,
            IGetsTriggersForWorkflowBlueprints getsTriggersForWorkflows)
        {
            _triggerStore = triggerStore;
            _mediator = mediator;
            _workflowProviders = workflowProviders;
            _logger = logger;
            _getsTriggersForWorkflows = getsTriggersForWorkflows;
        }

        public async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
        {
            var workflowBlueprints = await GetStaticWorkflowBlueprintsAsync(cancellationToken).ToListAsync(cancellationToken);
            await IndexTriggersAsync(workflowBlueprints, cancellationToken);
            await _mediator.Publish(new TriggerIndexingFinished(), cancellationToken);
        }

        public async Task IndexTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints, CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            _logger.LogInformation("Indexing triggers");

            foreach (var workflowBlueprint in workflowBlueprints)
                await IndexTriggersAsync(workflowBlueprint, cancellationToken);

            _stopwatch.Stop();
            _logger.LogInformation("Indexed triggers in {ElapsedTime}", _stopwatch.Elapsed);
        }

        public async Task IndexTriggersAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default)
        {
            // Delete existing triggers.
            await DeleteTriggersAsync(workflowBlueprint.Id, cancellationToken);

            // Get new triggers.
            var triggers = (await _getsTriggersForWorkflows.GetTriggersAsync(workflowBlueprint, cancellationToken)).ToList();

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
        
        public async Task DeleteTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowDefinitionIdSpecification(workflowDefinitionId);
            var count = await _triggerStore.DeleteManyAsync(specification, cancellationToken);
            _logger.LogDebug("Deleted {DeletedTriggerCount} triggers for workflow {WorkflowDefinitionId}", count, workflowDefinitionId);
        }

        private async IAsyncEnumerable<IWorkflowBlueprint> GetStaticWorkflowBlueprintsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var staticWorkflowProviders = _workflowProviders.Where(SkipDynamicProviders).ToList();

            foreach (var workflowProvider in staticWorkflowProviders)
            {
                var workflowBlueprints = workflowProvider.ListAsync(VersionOptions.Published, cancellationToken: cancellationToken);

                await foreach (var workflowBlueprint in workflowBlueprints.WithCancellation(cancellationToken))
                    yield return workflowBlueprint;
            }
        }
    }
}