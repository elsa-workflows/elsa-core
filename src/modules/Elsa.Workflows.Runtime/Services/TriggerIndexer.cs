using System.Runtime.CompilerServices;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Comparers;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class TriggerIndexer : ITriggerIndexer
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
        _logger = logger;
        _workflowDefinitionService = workflowDefinitionService;
    }

    /// <inheritdoc />
    public async Task DeleteTriggersAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var triggers = (await _triggerStore.FindManyAsync(filter, cancellationToken)).ToList();
        var workflowDefinitionVersionIds = triggers.Select(x => x.WorkflowDefinitionVersionId).Distinct().ToList();

        foreach (string workflowDefinitionVersionId in workflowDefinitionVersionIds)
        {
            var workflowGraph = await _workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionVersionId, cancellationToken);

            if (workflowGraph == null)
                continue;
            
            await DeleteTriggersAsync(workflowGraph.Workflow, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<IndexedWorkflowTriggers> IndexTriggersAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var workflowGraph = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        return await IndexTriggersAsync(workflowGraph.Workflow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IndexedWorkflowTriggers> IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        // Get current triggers
        var currentTriggers = await GetCurrentTriggersAsync(workflow.Identity.DefinitionId, cancellationToken).ToList();

        // Collect new triggers **if the workflow is published**.
        var newTriggers = workflow.Publication.IsPublished
            ? await GetTriggersInternalAsync(workflow, cancellationToken).ToListAsync(cancellationToken)
            : new List<StoredTrigger>(0);

        // Diff triggers.
        var diff = Diff.For(currentTriggers, newTriggers, new WorkflowTriggerEqualityComparer());

        // Replace triggers for the specified workflow.
        await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        var indexedWorkflow = new IndexedWorkflowTriggers(workflow, diff.Added, diff.Removed, diff.Unchanged);

        // Publish event.
        await _notificationSender.SendAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);
        return indexedWorkflow;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StoredTrigger>> GetTriggersAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        return await GetTriggersInternalAsync(workflow, cancellationToken).ToListAsync(cancellationToken);
    }
    
    private async Task DeleteTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        var emptyTriggerList = new List<StoredTrigger>(0);
        var currentTriggers = await GetCurrentTriggersAsync(workflow.Identity.DefinitionId, cancellationToken).ToList();
        var diff = Diff.For(currentTriggers, emptyTriggerList, new WorkflowTriggerEqualityComparer());
        await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);
        var indexedWorkflow = new IndexedWorkflowTriggers(workflow, emptyTriggerList, currentTriggers, emptyTriggerList);
        await _notificationSender.SendAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);
    }

    private async Task<IEnumerable<StoredTrigger>> GetCurrentTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken)
    {
        var filter = new TriggerFilter
        {
            WorkflowDefinitionId = workflowDefinitionId
        };
        return await _triggerStore.FindManyAsync(filter, cancellationToken);
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
        var triggerTypeName = trigger.Type;
        var triggerDescriptor = _activityRegistry.Find(triggerTypeName, trigger.Version);
        
        if (triggerDescriptor == null)
        {
            _logger.LogWarning("Could not find activity descriptor for activity type {ActivityType}", triggerTypeName);
            return new List<StoredTrigger>(0);
        }
        
        var expressionExecutionContext = await trigger.CreateExpressionExecutionContextAsync(triggerDescriptor, _serviceProvider, context, _expressionEvaluator, _logger);
        var triggerIndexingContext = new TriggerIndexingContext(context, expressionExecutionContext, trigger, cancellationToken);
        var triggerData = await TryGetTriggerDataAsync(trigger, triggerIndexingContext);

        // If no trigger payloads were returned, create a null payload.
        if (!triggerData.Any()) triggerData.Add(null!);

        var triggers = triggerData.Select(payload => new StoredTrigger
        {
            Id = _identityGenerator.GenerateId(),
            WorkflowDefinitionId = workflow.Identity.DefinitionId,
            WorkflowDefinitionVersionId = workflow.Identity.Id,
            Name = triggerTypeName,
            ActivityId = trigger.Id,
            Hash = _hasher.Hash(triggerTypeName, payload),
            Payload = payload
        });

        return triggers.ToList();
    }

    private async Task<List<object>> TryGetTriggerDataAsync(ITrigger trigger, TriggerIndexingContext context)
    {
        try
        {
            return (await trigger.GetTriggerPayloadsAsync(context)).ToList();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to get trigger data for activity {ActivityId}", trigger.Id);
        }

        return new List<object>(0);
    }
}