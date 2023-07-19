using System.Runtime.CompilerServices;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Comparers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class TriggerIndexer : ITriggerIndexer
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ITriggerStore _triggerStore;
    private readonly INotificationSender _notificationSender;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBookmarkHasher _hasher;
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
        INotificationSender notificationSender,
        IServiceProvider serviceProvider,
        IBookmarkHasher hasher,
        ILogger<TriggerIndexer> logger)
    {
        _activityVisitor = activityVisitor;
        _expressionEvaluator = expressionEvaluator;
        _identityGenerator = identityGenerator;
        _triggerStore = triggerStore;
        _notificationSender = notificationSender;
        _serviceProvider = serviceProvider;
        _hasher = hasher;
        _logger = logger;
        _workflowDefinitionService = workflowDefinitionService;
    }

    /// <inheritdoc />
    public async Task<IndexedWorkflowTriggers> IndexTriggersAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        return await IndexTriggersAsync(workflow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IndexedWorkflowTriggers> IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        // Get current triggers
        var currentTriggers = await GetCurrentTriggersAsync(workflow.Identity.DefinitionId, cancellationToken).ToList();

        // Collect new triggers **if workflow is published**.
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
    public async Task<IEnumerable<StoredTrigger>> GetTriggersAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        return await GetTriggersAsync(workflow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StoredTrigger>> GetTriggersAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        return await GetTriggersInternalAsync(workflow, cancellationToken).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IndexedWorkflowTriggers> IndexTriggersDeleteWorkflowAsync(ICollection<string> workflowDefinitionIds, CancellationToken cancellationToken = default)
    {
        var emptyTriggerList = new List<StoredTrigger>(0);

        // Get current triggers
        var currentTriggers = await GetCurrentTriggersAsync(workflowDefinitionIds, cancellationToken).ToList();

        // Diff triggers.
        var diff = Diff.For(currentTriggers, emptyTriggerList, new WorkflowTriggerEqualityComparer());

        // Replace triggers for the specified workflow.
        await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        //workflow definition already deleted so you do not have one
        var workflow = new Workflow();

        var indexedWorkflow = new IndexedWorkflowTriggers(workflow, emptyTriggerList, currentTriggers, emptyTriggerList);

        // Publish event.
        await _notificationSender.SendAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);

        return indexedWorkflow;
    }

    private async Task<IEnumerable<StoredTrigger>> GetCurrentTriggersAsync(ICollection<string> workflowDefinitionIds, CancellationToken cancellationToken)
    {
        var filter = new TriggerFilter { WorkflowDefinitionIds = workflowDefinitionIds };
        return await _triggerStore.FindManyAsync(filter, cancellationToken);
    }

    private async Task<IEnumerable<StoredTrigger>> GetCurrentTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken)
    {
        var filter = new TriggerFilter { WorkflowDefinitionId = workflowDefinitionId };
        return await _triggerStore.FindManyAsync(filter, cancellationToken);
    }

    private async IAsyncEnumerable<StoredTrigger> GetTriggersInternalAsync(Workflow workflow, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = new WorkflowIndexingContext(workflow, cancellationToken);
        var nodes = await _activityVisitor.VisitAsync(workflow.Root, cancellationToken);

        // Get a list of activities that are configured as "startable".
        var startableNodes = nodes
            .Flatten()
            .Where(x => x.Activity.GetCanStartWorkflow())
            .ToList();

        // For each startable node, create triggers.
        foreach (var node in startableNodes)
        {
            var triggers = await GetTriggersAsync(context, node.Activity);

            foreach (var trigger in triggers)
                yield return trigger;
        }
    }

    private async Task<IEnumerable<StoredTrigger>> GetTriggersAsync(WorkflowIndexingContext context, IActivity activity)
    {
        // If the activity implements ITrigger, request its trigger data. Otherwise.
        if (activity is ITrigger trigger)
            return await CreateWorkflowTriggersAsync(context, trigger);

        // Else, create a single workflow trigger with no additional data.
        var simpleTrigger = CreateWorkflowTrigger(context, activity);

        return new[] { simpleTrigger };
    }

    private StoredTrigger CreateWorkflowTrigger(WorkflowIndexingContext context, IActivity activity)
    {
        var workflow = context.Workflow;
        return new StoredTrigger
        {
            Id = _identityGenerator.GenerateId(),
            WorkflowDefinitionId = workflow.Identity.DefinitionId,
            WorkflowDefinitionVersionId = workflow.Identity.Id,
            Name = activity.Type,
            ActivityId = activity.Id
        };
    }

    private async Task<ICollection<StoredTrigger>> CreateWorkflowTriggersAsync(WorkflowIndexingContext context, ITrigger trigger)
    {
        var workflow = context.Workflow;
        var cancellationToken = context.CancellationToken;
        var expressionExecutionContext = await trigger.CreateExpressionExecutionContextAsync(_serviceProvider, context, _expressionEvaluator, _logger);

        var triggerIndexingContext = new TriggerIndexingContext(context, expressionExecutionContext, trigger, cancellationToken);
        var triggerData = await TryGetTriggerDataAsync(trigger, triggerIndexingContext);
        var triggerTypeName = trigger.Type;

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

    private async Task<ICollection<object>> TryGetTriggerDataAsync(ITrigger trigger, TriggerIndexingContext context)
    {
        try
        {
            return (await trigger.GetTriggerPayloadsAsync(context)).ToList();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to get trigger data for activity {ActivityId}", trigger.Id);
        }

        return Array.Empty<object>();
    }
}