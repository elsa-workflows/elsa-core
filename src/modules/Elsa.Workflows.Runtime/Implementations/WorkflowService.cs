using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowInvoker _workflowInvoker;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IWorkflowInstructionExecutor _workflowInstructionExecutor;
    private readonly IStimulusInterpreter _stimulusInterpreter;

    public WorkflowService(
        IWorkflowInvoker workflowInvoker,
        IWorkflowDispatcher workflowDispatcher,
        IWorkflowInstructionExecutor workflowInstructionExecutor, 
        IStimulusInterpreter stimulusInterpreter)
    {
        _workflowInvoker = workflowInvoker;
        _workflowDispatcher = workflowDispatcher;
        _workflowInstructionExecutor = workflowInstructionExecutor;
        _stimulusInterpreter = stimulusInterpreter;
    }

    public async Task<InvokeWorkflowResult> ExecuteWorkflowAsync(string definitionId, VersionOptions versionOptions, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
    {
        var executeRequest = new InvokeWorkflowDefinitionRequest(definitionId, versionOptions, input, correlationId);
        return await _workflowInvoker.InvokeAsync(executeRequest, cancellationToken);
    }

    public async Task<InvokeWorkflowResult> ExecuteWorkflowAsync(string instanceId, Bookmark bookmark, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
    {
        var request = new InvokeWorkflowInstanceRequest(instanceId, bookmark, input, correlationId);
        return await _workflowInvoker.InvokeAsync(request, cancellationToken);
    }

    public async Task<DispatchWorkflowDefinitionResponse> DispatchWorkflowAsync(string definitionId, VersionOptions versionOptions, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
    {
        var executeRequest = new DispatchWorkflowDefinitionRequest(definitionId, versionOptions, input, correlationId);
        return await _workflowDispatcher.DispatchAsync(executeRequest, cancellationToken);
    }

    public async Task<DispatchWorkflowInstanceResponse> DispatchWorkflowAsync(string instanceId, Bookmark bookmark, IDictionary<string, object>? input = default, string? correlationId = default, CancellationToken cancellationToken = default)
    {
        var request = new DispatchWorkflowInstanceRequest(instanceId, bookmark, input, correlationId);
        return await _workflowDispatcher.DispatchAsync(request, cancellationToken);
    }

    public async Task<IEnumerable<ExecuteWorkflowInstructionResult>> ExecuteStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken = default)
    {
        // Collect instructions for the specified stimulus.
        var instructions = await GetWorkflowInstructionsAsync(stimulus, cancellationToken);

        // Execute instructions.
        return await _workflowInstructionExecutor.ExecuteInstructionsAsync(instructions, cancellationToken);
    }

    public async Task<IEnumerable<DispatchWorkflowInstructionResult>> DispatchStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken)
    {
        // Collect instructions for the specified stimulus.
        var instructions = await GetWorkflowInstructionsAsync(stimulus, cancellationToken);

        // Execute instructions.
        return await _workflowInstructionExecutor.DispatchInstructionsAsync(instructions, cancellationToken);
    }

    private async Task<IEnumerable<IWorkflowInstruction>> GetWorkflowInstructionsAsync(IStimulus stimulus, CancellationToken cancellationToken = default) =>
        await _stimulusInterpreter.GetExecutionInstructionsAsync(stimulus, cancellationToken);
}