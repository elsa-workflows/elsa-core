using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Events;
using Elsa.Models;
using Elsa.MultiTenancy;
using Elsa.Options;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Triggers;
using Elsa.Providers.Workflows;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using Rebus.Extensions;

namespace Elsa.Services.Triggers
{
    public class TriggerIndexer : ITriggerIndexer
    {
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly IMediator _mediator;
        private readonly IIdGenerator _idGenerator;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();
        private readonly IGetsTriggersForWorkflowBlueprints _getsTriggersForWorkflows;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantStore _tenantStore;

        public TriggerIndexer(
            IBookmarkSerializer bookmarkSerializer,
            IMediator mediator,
            IIdGenerator idGenerator,
            ElsaOptions elsaOptions,
            ILogger<TriggerIndexer> logger,
            IGetsTriggersForWorkflowBlueprints getsTriggersForWorkflows,
            IServiceScopeFactory scopeFactory,
            ITenantStore tenantStore)
        {
            _bookmarkSerializer = bookmarkSerializer;
            _mediator = mediator;
            _idGenerator = idGenerator;
            _elsaOptions = elsaOptions;
            _logger = logger;
            _getsTriggersForWorkflows = getsTriggersForWorkflows;
            _scopeFactory = scopeFactory;
            _tenantStore = tenantStore;
        }

        public async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                var workflowBlueprints = await GetWorkflowBlueprintsAsync(scope.ServiceProvider, cancellationToken).ToListAsync(cancellationToken);
                await IndexTriggersAsync(workflowBlueprints, scope.ServiceProvider, cancellationToken);
            }
        }

        public async Task IndexTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints, Tenant tenant, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScopeForTenant(tenant);

            await IndexTriggersAsync(workflowBlueprints, scope.ServiceProvider, cancellationToken);
        }

        public async Task IndexTriggersAsync(IWorkflowBlueprint workflowBlueprint, Tenant tenant, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScopeForTenant(tenant);

            await IndexTriggersAsync(workflowBlueprint, scope.ServiceProvider, cancellationToken);
        }

        public async Task DeleteTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();

            await DeleteTriggersAsync(workflowDefinitionId, scope.ServiceProvider, cancellationToken);
        }

        private async IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowBlueprintsAsync(IServiceProvider serviceProvider, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var allWorkflowProviders = serviceProvider.GetRequiredService<IEnumerable<IWorkflowProvider>>();

            var excludedProviderTypes = _elsaOptions.WorkflowTriggerIndexingOptions.ExcludedProviders;
            var workflowProviders = allWorkflowProviders.Where(x => !excludedProviderTypes.Contains(x.GetType())).ToList();

            foreach (var workflowProvider in workflowProviders)
            {
                var workflowBlueprints = workflowProvider.ListAsync(VersionOptions.Published, cancellationToken: cancellationToken);

                await foreach (var workflowBlueprint in workflowBlueprints.WithCancellation(cancellationToken))
                    yield return workflowBlueprint;
            }
        }

        private async Task IndexTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            _logger.LogInformation("Indexing triggers");

            foreach (var workflowBlueprint in workflowBlueprints)
                await IndexTriggersAsync(workflowBlueprint, serviceProvider, cancellationToken);

            _stopwatch.Stop();
            _logger.LogInformation("Indexed triggers in {ElapsedTime}", _stopwatch.Elapsed);
        }

        private async Task IndexTriggersAsync(IWorkflowBlueprint workflowBlueprint, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();

            // Delete existing triggers.
            await DeleteTriggersAsync(workflowBlueprint.Id, serviceProvider, cancellationToken);

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
                await triggerStore.SaveAsync(trigger, cancellationToken);
            }

            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();
            var tenant = tenantProvider.GetCurrentTenant();

            // Publish event.
            await _mediator.Publish(new TriggerIndexingFinished(workflowBlueprint.Id, triggers, tenant), cancellationToken);
        }

        private async Task DeleteTriggersAsync(string workflowDefinitionId, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();

            var specification = new WorkflowDefinitionIdSpecification(workflowDefinitionId);
            var triggers = await triggerStore.FindManyAsync(specification, cancellationToken: cancellationToken).ToList();
            var count = triggers.Count;

            // Delete triggers.
            await triggerStore.DeleteManyAsync(specification, cancellationToken);

            // Publish event.
            await _mediator.Publish(new TriggersDeleted(workflowDefinitionId, triggers), cancellationToken);

            _logger.LogDebug("Deleted {DeletedTriggerCount} triggers for workflow {WorkflowDefinitionId}", count, workflowDefinitionId);
        }
    }
}