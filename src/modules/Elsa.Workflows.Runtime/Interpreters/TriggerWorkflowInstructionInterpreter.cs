using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Abstractions;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Interpreters;

public record TriggerWorkflowInstruction(WorkflowTrigger WorkflowTrigger, IDictionary<string, object>? Input, string? CorrelationId) : IWorkflowInstruction;

public class TriggerWorkflowInstructionInterpreter : WorkflowInstructionInterpreter<TriggerWorkflowInstruction>
{
    private readonly IWorkflowInvoker _workflowInvoker;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly ILogger _logger;

    public TriggerWorkflowInstructionInterpreter(
        IWorkflowInvoker workflowInvoker,
        IWorkflowDispatcher workflowDispatcher,
        IWorkflowDefinitionStore workflowDefinitionStore, 
        ILogger<TriggerWorkflowInstructionInterpreter> logger)
    {
        _workflowInvoker = workflowInvoker;
        _workflowDispatcher = workflowDispatcher;
        _workflowDefinitionStore = workflowDefinitionStore;
        _logger = logger;
    }

    protected override async ValueTask<ExecuteWorkflowInstructionResult?> ExecuteInstructionAsync(TriggerWorkflowInstruction instruction, CancellationToken cancellationToken = default)
    {
        var workflowTrigger = instruction.WorkflowTrigger;
        var definitionId = workflowTrigger.WorkflowDefinitionId;

        // Check if the workflow definition exists.
        var exists = await GetDefinitionExistsAsync(definitionId, cancellationToken);

        if (!exists)
            return null;

        // Execute workflow.
        var executeRequest = new InvokeWorkflowDefinitionRequest(definitionId, VersionOptions.Published, instruction.Input, instruction.CorrelationId);
        var workflowExecutionResult = await _workflowInvoker.InvokeAsync(executeRequest, cancellationToken);

        return new ExecuteWorkflowInstructionResult(workflowExecutionResult);
    }

    protected override async ValueTask<DispatchWorkflowInstructionResult?> DispatchInstructionAsync(TriggerWorkflowInstruction instruction, CancellationToken cancellationToken = default)
    {
        var workflowTrigger = instruction.WorkflowTrigger;
        var definitionId = workflowTrigger.WorkflowDefinitionId;

        // Check if the workflow definition exists.
        var exists = await GetDefinitionExistsAsync(definitionId, cancellationToken);

        if (!exists)
            return null;

        // Execute workflow.
        var dispatchRequest = new DispatchWorkflowDefinitionRequest(definitionId, VersionOptions.Published, instruction.Input, instruction.CorrelationId);
        await _workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken);

        return new DispatchWorkflowInstructionResult();
    }

    private async Task<bool> GetDefinitionExistsAsync(string definitionId, CancellationToken cancellationToken) => await _workflowDefinitionStore.GetExistsAsync(definitionId, VersionOptions.Published, cancellationToken);
}