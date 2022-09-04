using System.Runtime.CompilerServices;
using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Comparers;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.Implementations;

public class TriggerIndexer : ITriggerIndexer
{
    private readonly IActivityWalker _activityWalker;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IWorkflowTriggerStore _workflowTriggerStore;
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHasher _hasher;
    private readonly ILogger _logger;

    public TriggerIndexer(
        IActivityWalker activityWalker,
        IWorkflowDefinitionService workflowDefinitionService,
        IExpressionEvaluator expressionEvaluator,
        IIdentityGenerator identityGenerator,
        IWorkflowTriggerStore workflowTriggerStore,
        IEventPublisher eventPublisher,
        IServiceProvider serviceProvider,
        IHasher hasher,
        ILogger<TriggerIndexer> logger)
    {
        _activityWalker = activityWalker;
        _expressionEvaluator = expressionEvaluator;
        _identityGenerator = identityGenerator;
        _workflowTriggerStore = workflowTriggerStore;
        _eventPublisher = eventPublisher;
        _serviceProvider = serviceProvider;
        _hasher = hasher;
        _logger = logger;
        _workflowDefinitionService = workflowDefinitionService;
    }

    public async Task<IndexedWorkflowTriggers> IndexTriggersAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        return await IndexTriggersAsync(workflow, cancellationToken);
    }

    public async Task<IndexedWorkflowTriggers> IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        // Get current triggers
        var currentTriggers = await GetCurrentTriggersAsync(workflow.Identity.DefinitionId, cancellationToken).ToList();

        // Collect new triggers **if workflow is published**.
        var newTriggers = workflow.Publication.IsPublished
            ? await GetTriggersAsync(workflow, cancellationToken).ToListAsync(cancellationToken)
            : new List<WorkflowTrigger>(0);

        // Diff triggers.
        var diff = Diff.For(currentTriggers, newTriggers, new WorkflowTriggerHashEqualityComparer());

        // Replace triggers for the specified workflow.
        await _workflowTriggerStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        var indexedWorkflow = new IndexedWorkflowTriggers(workflow, diff.Added, diff.Removed, diff.Unchanged);

        // Publish event.
        await _eventPublisher.PublishAsync(new WorkflowTriggersIndexed(indexedWorkflow), cancellationToken);
        return indexedWorkflow;
    }

    private async Task<IEnumerable<WorkflowTrigger>> GetCurrentTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken) =>
        await _workflowTriggerStore.FindManyByWorkflowDefinitionIdAsync(workflowDefinitionId, cancellationToken);

    private async IAsyncEnumerable<WorkflowTrigger> GetTriggersAsync(Workflow workflow, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = new WorkflowIndexingContext(workflow, cancellationToken);
        var nodes = await _activityWalker.WalkAsync(workflow.Root, cancellationToken);
        
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

    private async Task<IEnumerable<WorkflowTrigger>> GetTriggersAsync(WorkflowIndexingContext context, IActivity activity)
    {
        // If the activity implements ITrigger, request its trigger data. Otherwise, create one trigger datum.
        if (activity is ITrigger trigger)
            return await CreateWorkflowTriggersAsync(context, trigger);

        // Else, create a single workflow trigger with no additional data.
        var simpleTrigger = CreateWorkflowTrigger(context, activity);

        return new[] { simpleTrigger };
    }

    private WorkflowTrigger CreateWorkflowTrigger(WorkflowIndexingContext context, IActivity activity)
    {
        var workflow = context.Workflow;
        return new WorkflowTrigger
        {
            Id = _identityGenerator.GenerateId(),
            WorkflowDefinitionId = workflow.Identity.DefinitionId,
            Name = activity.Type
        };
    }

    private async Task<ICollection<WorkflowTrigger>> CreateWorkflowTriggersAsync(WorkflowIndexingContext context, ITrigger trigger)
    {
        var workflow = context.Workflow;
        var cancellationToken = context.CancellationToken;
        var expressionExecutionContext = await CreateExpressionExecutionContextAsync(context, trigger);

        var triggerIndexingContext = new TriggerIndexingContext(context, expressionExecutionContext, trigger, cancellationToken);
        var triggerData = await TryGetTriggerDataAsync(trigger, triggerIndexingContext);
        var triggerTypeName = trigger.Type;

        var triggers = triggerData.Select(x => new WorkflowTrigger
        {
            Id = _identityGenerator.GenerateId(),
            WorkflowDefinitionId = workflow.Identity.DefinitionId,
            Name = triggerTypeName,
            Hash = _hasher.Hash(x),
            Data = JsonSerializer.Serialize(x)
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
        var transientProperties = new Dictionary<object, object>();
        var applicationProperties = ExpressionExecutionContextExtensions.CreateApplicationPropertiesFrom(context.Workflow, transientProperties, expressionInput);
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, register, default, applicationProperties, cancellationToken);

        // Evaluate activity inputs before requesting trigger data.
        foreach (var input in assignedInputs)
        {
            var locationReference = input.MemoryBlockReference;

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
            return (await trigger.GetTriggerDataAsync(context)).ToList();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to get trigger data for activity {ActivityId}", trigger.Id);
        }

        return Array.Empty<object>();
    }
}