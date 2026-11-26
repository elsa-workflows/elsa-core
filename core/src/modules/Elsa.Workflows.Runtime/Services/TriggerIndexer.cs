using System.Runtime.CompilerServices;
using Elsa.Caching;
using Elsa.Common.Multitenancy;
using Elsa.Common.DistributedHosting;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Comparers;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Stores;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Linq.AsyncExtensions;
using Elsa.Common.Serialization;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public partial class TriggerIndexer : ITriggerIndexer
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ITriggerStore _triggerStore;
    private readonly IActivityRegistry _activityRegistry;
    private readonly INotificationSender _notificationSender;
    private readonly IServiceProvider _serviceProvider;
    private readonly IStimulusHasher _hasher;
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly WorkflowTriggerEqualityComparer _triggerEqualityComparer;
    private readonly DistributedLockingOptions _lockingOptions;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public TriggerIndexer(
        IActivityVisitor activityVisitor,
        IWorkflowDefinitionService workflowDefinitionService,
        IExpressionEvaluator expressionEvaluator,
        IIdentityGenerator identityGenerator,
        ITriggerStore triggerStore,
        IActivityRegistry activityRegistry,
        INotificationSender notificationSender,
        IServiceProvider serviceProvider,
        IStimulusHasher hasher,
        IDistributedLockProvider distributedLockProvider,
        ISerializationTypeRegistry workflowJsonTypeRegistry,
        IOptions<DistributedLockingOptions> lockingOptions,
        ILogger<TriggerIndexer> logger)
    {
        _activityVisitor = activityVisitor;
        _expressionEvaluator = expressionEvaluator;
        _identityGenerator = identityGenerator;
        _triggerStore = triggerStore;
        _activityRegistry = activityRegistry;
        _notificationSender = notificationSender;
        _serviceProvider = serviceProvider;
        _hasher = hasher;
        _distributedLockProvider = distributedLockProvider;
        _triggerEqualityComparer = new WorkflowTriggerEqualityComparer(workflowJsonTypeRegistry);
        _lockingOptions = lockingOptions.Value;
        _logger = logger;
        _workflowDefinitionService = workflowDefinitionService;
    }

    /// <inheritdoc />
    public async Task DeleteTriggersAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var triggers = await FindTriggersToDeleteAsync(filter, cancellationToken);

        foreach (var group in triggers.GroupBy(trigger => trigger.WorkflowDefinitionVersionId))
        {
            try
            {
                var workflowGraph = await _workflowDefinitionService.FindWorkflowGraphAsync(group.Key, cancellationToken);
                if (workflowGraph == null)
                {
                    if (filter.RequireCompleteCleanup)
                    {
                        throw new InvalidOperationException("The workflow graph is unavailable for trigger cleanup.");
                    }

                    continue;
                }

                // Without the authority to delete tenant-agnostic definitions, the version is not deleted: these are the tenant's copies of its triggers.
                if (IsSharedDefinition(workflowGraph.Workflow) && !SharedTenantWriteScope.IsGranted(typeof(WorkflowDefinition)))
                    continue;

                await RemoveTriggersAsync(workflowGraph.Workflow, group.ToList(), filter.RequireCompleteCleanup, cancellationToken);
            }
            catch (Exception ex) when (!filter.RequireCompleteCleanup && ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to clean up triggers for workflow definition version {WorkflowDefinitionVersionId}.", group.Key);
            }
        }

        await DeleteTenantCopiesOfDeletedSharedDefinitionsAsync(filter, cancellationToken);

        // Retry this signal even when the trigger rows were removed by a prior attempt.
        if (filter.RequireCompleteCleanup && _serviceProvider.GetService<ICacheManager>() is { } cacheManager)
        {
            await cacheManager.TriggerTokenAsync(CachingTriggerStore.RequiredRemovalCacheInvalidationTokenKey);
        }
    }

    /// <inheritdoc />
    public Task<IndexedWorkflowTriggers> IndexTriggersAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        return IndexTriggersAsync(definition, true, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The scheduled triggers of a tenant-agnostic workflow definition are reconciled for every active tenant.
    /// </remarks>
    public Task<IndexedWorkflowTriggers> IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        return IndexTriggersAsync(workflow, true, cancellationToken);
    }

    /// <summary>
    /// Indexes the triggers of the specified workflow definition. For a tenant-agnostic definition, <paramref name="allTenants"/> selects
    /// whether its scheduled triggers are reconciled for every active tenant or for the current tenant only.
    /// </summary>
    internal async Task<IndexedWorkflowTriggers> IndexTriggersAsync(WorkflowDefinition definition, bool allTenants, CancellationToken cancellationToken)
    {
        var workflowGraph = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        return await IndexTriggersAsync(workflowGraph.Workflow, allTenants, cancellationToken);
    }

    /// <inheritdoc cref="IndexTriggersAsync(WorkflowDefinition, bool, CancellationToken)"/>
    internal async Task<IndexedWorkflowTriggers> IndexTriggersAsync(Workflow workflow, bool allTenants, CancellationToken cancellationToken)
    {
        // Use distributed lock to prevent concurrent trigger indexing race conditions
        await using (await AcquireDefinitionLockAsync(workflow.Identity.DefinitionId, cancellationToken))
        {
            return IsSharedDefinition(workflow)
                ? await IndexSharedDefinitionTriggersAsync(workflow, allTenants, cancellationToken)
                : await IndexTriggersInternalAsync(workflow, cancellationToken);
        }
    }

    private async Task<IAsyncDisposable> AcquireDefinitionLockAsync(string definitionId, CancellationToken cancellationToken)
    {
        var lockResource = $"trigger-indexer:{definitionId}";
        return await _distributedLockProvider.AcquireLockAsync(lockResource, _lockingOptions.LockAcquisitionTimeout, cancellationToken);
    }

    private async Task<IndexedWorkflowTriggers> IndexTriggersInternalAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        // Get current triggers
        var currentTriggers = await GetCurrentTriggersAsync(workflow.Identity.DefinitionId, cancellationToken).ToList();

        // Collect new triggers **if the workflow is published**.
        var newTriggers = workflow.Publication.IsPublished
            ? await GetTriggersInternalAsync(workflow, cancellationToken).ToListAsync(cancellationToken)
            : new(0);

        // Diff triggers.
        var diff = Diff.For(currentTriggers, newTriggers, _triggerEqualityComparer);

        // Replace triggers for the specified workflow.
        await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        var indexedWorkflow = new IndexedWorkflowTriggers(workflow, diff.Added, diff.Removed, diff.Unchanged);

        // Publish event.
        await _notificationSender.SendAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);

        // A tenant whose own definition is no longer published stops overriding a tenant-agnostic definition with the same ID.
        if (IsTenancyEnabled && !workflow.Publication.IsPublished)
            await ReconcileTenantCopiesOfPublishedSharedDefinitionAsync(workflow.Identity.DefinitionId, false, cancellationToken);

        return indexedWorkflow;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StoredTrigger>> GetTriggersAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        return await GetTriggersInternalAsync(workflow, cancellationToken).ToListAsync(cancellationToken);
    }
    
    private async Task<IEnumerable<StoredTrigger>> GetCurrentTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken)
    {
        var filter = new TriggerFilter
        {
            WorkflowDefinitionId = workflowDefinitionId
        };
        var triggers = await _triggerStore.FindManyAsync(filter, cancellationToken);

        // The tenant's view includes the rows of tenant-agnostic definitions, which are never the tenant's own current rows.
        if (!IsTenancyEnabled)
            return triggers;

        var tenantId = CurrentTenantId;
        return triggers.Where(x => IsOwnedBy(x, tenantId)).ToList();
    }

    private async IAsyncEnumerable<StoredTrigger> GetTriggersInternalAsync(Workflow workflow, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = new WorkflowIndexingContext(workflow, cancellationToken);
        var nodes = await _activityVisitor.VisitAsync(workflow.Root, cancellationToken);

        // Get a list of trigger activities that are configured as "startable".
        var triggerActivities = nodes
            .Flatten()
            .Where(x => x.Activity.GetCanStartWorkflow() && x.Activity is ITrigger)
            .Select(x => x.Activity)
            .Cast<ITrigger>()
            .ToList();

        // For each trigger activity, create a trigger.
        foreach (var triggerActivity in triggerActivities)
        {
            var triggers = await CreateWorkflowTriggersAsync(context, triggerActivity);

            foreach (var trigger in triggers)
                yield return trigger;
        }
    }

    private async Task<ICollection<StoredTrigger>> CreateWorkflowTriggersAsync(WorkflowIndexingContext context, ITrigger trigger)
    {
        var workflow = context.Workflow;
        var cancellationToken = context.CancellationToken;
        var activityTypeName = trigger.Type;
        var triggerDescriptor = _activityRegistry.Find(activityTypeName, trigger.Version);
        
        if (triggerDescriptor == null)
        {
            _logger.LogWarning("Could not find activity descriptor for activity type {ActivityType}", activityTypeName);
            return new List<StoredTrigger>(0);
        }
        
        var expressionExecutionContext = await trigger.CreateExpressionExecutionContextAsync(triggerDescriptor, _serviceProvider, context, _expressionEvaluator, _logger);
        var triggerIndexingContext = new TriggerIndexingContext(context, expressionExecutionContext, trigger, cancellationToken);
        var triggerData = await TryGetTriggerDataAsync(trigger, triggerIndexingContext);
        var defaultTriggerName = triggerIndexingContext.TriggerName;

        // A trigger that completed, returned no payloads, and declared that deliberate gets no row at all. One that threw never counts as having declined.
        if (triggerData is { Count: 0 } && triggerIndexingContext.RegistersNoTriggers)
            return new List<StoredTrigger>(0);

        triggerData ??= [];

        // If no trigger payloads were returned, create a null payload.
        if (!triggerData.Any()) triggerData.Add(null!);

        var triggers = triggerData.Select(payload =>
        {
            // A payload can carry its own stimulus name. If it does not, the trigger's shared name applies.
            // Name and payload are always taken from the same source so that the hash matches the name stored alongside it.
            var namedPayload = payload as NamedTriggerPayload;
            var triggerName = namedPayload != null ? namedPayload.Name : defaultTriggerName;
            var stimulus = namedPayload != null ? namedPayload.Payload : payload;

            return new StoredTrigger
            {
                Id = _identityGenerator.GenerateId(),
                WorkflowDefinitionId = workflow.Identity.DefinitionId,
                WorkflowDefinitionVersionId = workflow.Identity.Id,
                Name = triggerName,
                ActivityId = trigger.Id,
                Hash = _hasher.Hash(triggerName, stimulus),
                Payload = stimulus
            };
        });

        return triggers.ToList();
    }

    /// <summary>
    /// Returns the trigger's payloads, or <c>null</c> when it throws, so that a failure is told apart from a trigger that returned nothing.
    /// </summary>
    private async Task<List<object>?> TryGetTriggerDataAsync(ITrigger trigger, TriggerIndexingContext context)
    {
        try
        {
            return (await trigger.GetTriggerPayloadsAsync(context)).ToList();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to get trigger data for activity {ActivityId}", trigger.Id);
        }

        return null;
    }
}
