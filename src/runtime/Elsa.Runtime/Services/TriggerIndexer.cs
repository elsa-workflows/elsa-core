using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elsa.Contracts;
using Elsa.Extensions;
using Elsa.Helpers;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Runtime.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime.Services;

public class TriggerIndexer : ITriggerIndexer
{
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ICommandSender _mediator;
    private readonly IHasher _hasher;
    private readonly ILogger _logger;

    public TriggerIndexer(
        IWorkflowRegistry workflowRegistry,
        IExpressionEvaluator expressionEvaluator,
        ICommandSender mediator,
        IHasher hasher,
        ILogger<TriggerIndexer> logger)
    {
        _workflowRegistry = workflowRegistry;
        _expressionEvaluator = expressionEvaluator;
        _mediator = mediator;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = new Stopwatch();

        _logger.LogInformation("Indexing workflow triggers");
        stopwatch.Start();
        
        var workflows = _workflowRegistry.StreamAllAsync(cancellationToken);

        await foreach (var workflow in workflows.WithCancellation(cancellationToken))
            await IndexTriggersAsync(workflow, cancellationToken);
        
        stopwatch.Stop();
        _logger.LogInformation("Finished indexing workflow triggers in {ElapsedTime}", stopwatch.Elapsed);
    }

    public async Task IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        // Collect new triggers.
        var triggers = await GetTriggersAsync(workflow, cancellationToken).ToListAsync(cancellationToken);

        // Replace triggers for the specified workflow.
        var definitionId = workflow.Identity.DefinitionId;
        await _mediator.ExecuteAsync(new ReplaceWorkflowTriggers(definitionId, triggers), cancellationToken);
    }

    private async IAsyncEnumerable<WorkflowTrigger> GetTriggersAsync(Workflow workflow, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = new WorkflowIndexingContext(workflow);
        var triggerSources = workflow.Triggers;

        foreach (var triggerSource in triggerSources)
        {
            var triggers = await GetTriggersAsync(workflow, context, triggerSource, cancellationToken);

            foreach (var trigger in triggers)
                yield return trigger;
        }
    }

    private async Task<IEnumerable<WorkflowTrigger>> GetTriggersAsync(Workflow workflow, WorkflowIndexingContext context, ITrigger trigger, CancellationToken cancellationToken)
    {
        var inputs = trigger.GetInputs();
        var assignedInputs = inputs.Where(x => x.LocationReference != null!).ToList();
        var register = context.GetOrCreateRegister(trigger);
        var expressionExecutionContext = new ExpressionExecutionContext(register, default);

        // Evaluate trigger inputs.
        foreach (var input in assignedInputs)
        {
            var locationReference = input.LocationReference;
            var value = await _expressionEvaluator.EvaluateAsync(input, expressionExecutionContext);
            locationReference.Set(expressionExecutionContext, value);
        }

        var triggerIndexingContext = new TriggerIndexingContext(context, expressionExecutionContext, trigger);
        var hashInputs = await trigger.GetHashInputsAsync(triggerIndexingContext, cancellationToken);
        var triggerType = trigger.GetType();
        var triggerTypeName = TypeNameHelper.GenerateTypeName(triggerType);

        var triggers = hashInputs.Select(x => new WorkflowTrigger
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowDefinitionId = workflow.Identity.DefinitionId,
            Name = triggerTypeName,
            Hash = _hasher.Hash(x)
        });

        return triggers;
    }
}