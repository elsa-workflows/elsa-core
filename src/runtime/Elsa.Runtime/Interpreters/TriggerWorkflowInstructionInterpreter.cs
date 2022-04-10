using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Runtime.Abstractions;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime.Interpreters;

public record TriggerWorkflowInstruction(WorkflowTrigger WorkflowTrigger, IDictionary<string, object>? Input) : IWorkflowInstruction;

public class TriggerWorkflowInstructionInterpreter : WorkflowInstructionInterpreter<TriggerWorkflowInstruction>
{
    private readonly IWorkflowInvoker _workflowInvoker;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly ILogger _logger;

    public TriggerWorkflowInstructionInterpreter(
        IWorkflowInvoker workflowInvoker,
        IWorkflowDispatcher workflowDispatcher,
        IWorkflowRegistry workflowRegistry, 
        ILogger<TriggerWorkflowInstructionInterpreter> logger)
    {
        _workflowInvoker = workflowInvoker;
        _workflowDispatcher = workflowDispatcher;
        _workflowRegistry = workflowRegistry;
        _logger = logger;
    }

    protected override async ValueTask<ExecuteWorkflowInstructionResult?> ExecuteInstructionAsync(TriggerWorkflowInstruction instruction, CancellationToken cancellationToken = default)
    {
        var workflowTrigger = instruction.WorkflowTrigger;
        var workflowId = workflowTrigger.WorkflowDefinitionId;

        // Get workflow to execute.
        var workflow = await FindWorkflowAsync(workflowId, cancellationToken);

        if (workflow == null)
            return null;

        // Execute workflow.
        var executeRequest = new InvokeWorkflowDefinitionRequest(workflowId, VersionOptions.Published, instruction.Input);
        var workflowExecutionResult = await _workflowInvoker.InvokeAsync(executeRequest, cancellationToken);

        return new ExecuteWorkflowInstructionResult(workflow, workflowExecutionResult);
    }

    protected override async ValueTask<DispatchWorkflowInstructionResult?> DispatchInstructionAsync(TriggerWorkflowInstruction instruction, CancellationToken cancellationToken = default)
    {
        var workflowTrigger = instruction.WorkflowTrigger;
        var definitionId = workflowTrigger.WorkflowDefinitionId;

        // Get workflow to dispatch.
        var workflow = await FindWorkflowAsync(definitionId, cancellationToken);

        if (workflow == null)
            return null;

        // Execute workflow.
        var dispatchRequest = new DispatchWorkflowDefinitionRequest(definitionId, VersionOptions.Published, instruction.Input);
        await _workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken);

        return new DispatchWorkflowInstructionResult();
    }

    private async Task<Workflow?> FindWorkflowAsync(string id, CancellationToken cancellationToken)
    {
        // Get workflow to execute.
        var workflow = await _workflowRegistry.FindByIdAsync(id, VersionOptions.Published, cancellationToken);

        if (workflow != null)
            return workflow;

        _logger.LogWarning("Could not trigger workflow definition {WorkflowDefinitionId} because it was not found", id);
        return null;
    }
}