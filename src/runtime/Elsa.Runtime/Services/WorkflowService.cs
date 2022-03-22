using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;

namespace Elsa.Runtime.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowInvoker _workflowInvoker;
    private readonly IWorkflowInstructionExecutor _workflowInstructionExecutor;
    private readonly IStimulusInterpreter _stimulusInterpreter;

    public WorkflowService(IWorkflowInvoker workflowInvoker, IWorkflowInstructionExecutor workflowInstructionExecutor, IStimulusInterpreter stimulusInterpreter)
    {
        _workflowInvoker = workflowInvoker;
        _workflowInstructionExecutor = workflowInstructionExecutor;
        _stimulusInterpreter = stimulusInterpreter;
    }

    public async Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string definitionId, VersionOptions versionOptions, IReadOnlyDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var executeRequest = new ExecuteWorkflowDefinitionRequest(definitionId, versionOptions, input);
        return await _workflowInvoker.ExecuteAsync(executeRequest, cancellationToken);
    }

    public async Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string instanceId, Bookmark bookmark, IReadOnlyDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var request = new ExecuteWorkflowInstanceRequest(instanceId, bookmark, input);
        return await _workflowInvoker.ExecuteAsync(request, cancellationToken);
    }

    public async Task<DispatchWorkflowResult> DispatchWorkflowAsync(string definitionId, VersionOptions versionOptions, IReadOnlyDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var executeRequest = new DispatchWorkflowDefinitionRequest(definitionId, versionOptions, input);
        return await _workflowInvoker.DispatchAsync(executeRequest, cancellationToken);
    }

    public async Task<DispatchWorkflowResult> DispatchWorkflowAsync(string instanceId, Bookmark bookmark, IReadOnlyDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var request = new DispatchWorkflowInstanceRequest(instanceId, bookmark, input);
        return await _workflowInvoker.DispatchAsync(request, cancellationToken);
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