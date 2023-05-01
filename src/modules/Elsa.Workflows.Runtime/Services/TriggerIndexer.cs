using System.Runtime.CompilerServices;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Comparers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Models.Notifications;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using IEventPublisher = Elsa.Mediator.Contracts.IEventPublisher;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class TriggerIndexer : ITriggerIndexer
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ITriggerStore _triggerStore;
    private readonly IEventPublisher _eventPublisher;
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
        IEventPublisher eventPublisher,
        IServiceProvider serviceProvider,
        IBookmarkHasher hasher,
        ILogger<TriggerIndexer> logger)
    {
        _activityVisitor = activityVisitor;
        _expressionEvaluator = expressionEvaluator;
        _identityGenerator = identityGenerator;
        _triggerStore = triggerStore;
        _eventPublisher = eventPublisher;
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
            ? await GetTriggersAsync(workflow, cancellationToken).ToListAsync(cancellationToken)
            : new List<StoredTrigger>(0);

        // Diff triggers.
        var diff = Diff.For(currentTriggers, newTriggers, new WorkflowTriggerEqualityComparer());

        // Replace triggers for the specified workflow.
        await _triggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        var indexedWorkflow = new IndexedWorkflowTriggers(workflow, diff.Added, diff.Removed, diff.Unchanged);

        // Publish event.
        await _eventPublisher.PublishAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);
        return indexedWorkflow;
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
        await _eventPublisher.PublishAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);

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

    private async IAsyncEnumerable<StoredTrigger> GetTriggersAsync(Workflow workflow, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = new WorkflowIndexingContext(workflow, cancellationToken);
        var nodes = await _activityVisitor.VisitAsync(workflow.Root, cancellationToken);

        // Get a list of activities that are configured as "startable".
        var startableNodes = nodes
            .Flatten()
            .Where(x => x.Activity.CanStartWorkflow)
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
            Name = activity.Type,
            ActivityId = activity.Id
        };
    }

    private async Task<ICollection<StoredTrigger>> CreateWorkflowTriggersAsync(WorkflowIndexingContext context, ITrigger trigger)
    {
        var workflow = context.Workflow;
        var cancellationToken = context.CancellationToken;
        var expressionExecutionContext = await CreateExpressionExecutionContextAsync(context, trigger);

        var triggerIndexingContext = new TriggerIndexingContext(context, expressionExecutionContext, trigger, cancellationToken);
        var triggerData = await TryGetTriggerDataAsync(trigger, triggerIndexingContext);
        var triggerTypeName = trigger.Type;

        var triggers = triggerData.Select(payload => new StoredTrigger
        {
            Id = _identityGenerator.GenerateId(),
            WorkflowDefinitionId = workflow.Identity.DefinitionId,
            Name = triggerTypeName,
            ActivityId = trigger.Id,
            Hash = _hasher.Hash(triggerTypeName, payload),
            Payload = payload
        });

        return triggers.ToList();
    }

    private async Task<ExpressionExecutionContext> CreateExpressionExecutionContextAsync(WorkflowIndexingContext context, ITrigger trigger)
    {
        var inputs = trigger.GetInputs();
        var assignedInputs = inputs.Where(x => x.MemoryBlockReference != null!).ToList();
        var register = context.GetOrCreateRegister(trigger);
        var cancellationToken = context.CancellationToken;
        var expressionInput = new Dictionary<string, object>();
        var applicationProperties = ExpressionExecutionContextExtensions.CreateTriggerIndexingPropertiesFrom(context.Workflow, expressionInput);
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, register, default, applicationProperties, cancellationToken);

        // Evaluate activity inputs before requesting trigger data.
        foreach (var input in assignedInputs)
        {
            var locationReference = input.MemoryBlockReference();

            try
            {
                var value = await _expressionEvaluator.EvaluateAsync(input, expressionExecutionContext);
                locationReference.Set(expressionExecutionContext, value);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to evaluate '{@Expression}'", input.Expression);
            }
        }

        return expressionExecutionContext;
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