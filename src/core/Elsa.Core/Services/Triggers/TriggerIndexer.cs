using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Options;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Triggers;
using Elsa.Providers.Workflows;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using Rebus.Extensions;

namespace Elsa.Services.Triggers
{
    public class TriggerIndexer : ITriggerIndexer
    {
        private readonly ITriggerStore _triggerStore;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly IMediator _mediator;
        private readonly IIdGenerator _idGenerator;
        private readonly IEnumerable<IWorkflowProvider> _workflowProviders;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();
        private readonly IGetsTriggersForWorkflowBlueprints _getsTriggersForWorkflows;

        public TriggerIndexer(
            ITriggerStore triggerStore,
            IBookmarkSerializer bookmarkSerializer,
            IMediator mediator,
            IIdGenerator idGenerator,
            IEnumerable<IWorkflowProvider> workflowProviders,
            ElsaOptions elsaOptions,
            ILogger<TriggerIndexer> logger,
            IGetsTriggersForWorkflowBlueprints getsTriggersForWorkflows)
        {
            _triggerStore = triggerStore;
            _bookmarkSerializer = bookmarkSerializer;
            _mediator = mediator;
            _idGenerator = idGenerator;
            _workflowProviders = workflowProviders;
            _elsaOptions = elsaOptions;
            _logger = logger;
            _getsTriggersForWorkflows = getsTriggersForWorkflows;
        }

        public async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
        {
            var workflowBlueprints = await GetWorkflowBlueprintsAsync(cancellationToken).ToListAsync(cancellationToken);
            await IndexTriggersAsync(workflowBlueprints, cancellationToken);
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
            var workflowTriggers = (await _getsTriggersForWorkflows.GetTriggersAsync(workflowBlueprint, cancellationToken)).ToList();
            var triggers = new List<Trigger>();

            foreach (var workflowTrigger in workflowTriggers)
            {
                var bookmark = workflowTrigger.Bookmark;
                var trigger = new Trigger
                {
                    Id = _idGenerator.Generate(),
                    ActivityId = workflowTrigger.ActivityId,
                    ActivityType = workflowTrigger.ActivityType,
                    Hash = workflowTrigger.BookmarkHash,
                    TenantId = workflowTrigger.TenantId,
                    WorkflowDefinitionId = workflowTrigger.WorkflowDefinitionId,
                    Model = _bookmarkSerializer.Serialize(bookmark),
                    ModelType = bookmark.GetType().GetSimpleAssemblyQualifiedName()
                };

                triggers.Add(trigger);
                await _triggerStore.SaveAsync(trigger, cancellationToken);
            }

            // Publish event.
            await _mediator.Publish(new TriggerIndexingFinished(workflowBlueprint.Id, triggers), cancellationToken);
        }

        public async Task DeleteTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowDefinitionIdSpecification(workflowDefinitionId);
            var triggers = await _triggerStore.FindManyAsync(specification, cancellationToken: cancellationToken).ToList();
            var count = triggers.Count;

            // Delete triggers.
            await _triggerStore.DeleteManyAsync(specification, cancellationToken);

            // Publish event.
            await _mediator.Publish(new TriggersDeleted(workflowDefinitionId, triggers), cancellationToken);

            _logger.LogDebug("Deleted {DeletedTriggerCount} triggers for workflow {WorkflowDefinitionId}", count, workflowDefinitionId);
        }

        private async IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowBlueprintsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var excludedProviderTypes = _elsaOptions.WorkflowTriggerIndexingOptions.ExcludedProviders;
            var workflowProviders = _workflowProviders.Where(x => !excludedProviderTypes.Contains(x.GetType())).ToList();

            foreach (var workflowProvider in workflowProviders)
            {
                var workflowBlueprints = workflowProvider.ListAsync(VersionOptions.Published, cancellationToken: cancellationToken);

                await foreach (var workflowBlueprint in workflowBlueprints.WithCancellation(cancellationToken))
                    yield return workflowBlueprint;
            }
        }
    }
}