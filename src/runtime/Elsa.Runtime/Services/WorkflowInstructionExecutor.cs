using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Services;

public class WorkflowInstructionExecutor : IWorkflowInstructionExecutor
{
    private readonly IEnumerable<IWorkflowInstructionInterpreter> _workflowExecutionInstructionHandlers;

    public WorkflowInstructionExecutor(IEnumerable<IWorkflowInstructionInterpreter> workflowExecutionInstructionHandlers)
    {
        _workflowExecutionInstructionHandlers = workflowExecutionInstructionHandlers;
    }

    public async Task<IEnumerable<ExecuteWorkflowInstructionResult>> ExecuteInstructionAsync(IWorkflowInstruction instruction, CancellationToken cancellationToken = default)
    {
        var handlers = _workflowExecutionInstructionHandlers.Where(x => x.GetSupportsInstruction(instruction)).ToList();
        var tasks = handlers.Select(x => x.ExecuteInstructionAsync(instruction, cancellationToken).AsTask());
        var results = await Task.WhenAll(tasks);
        return results.Where(x => x != null).Select(x => x!).ToList();
    }

    public async Task<IEnumerable<ExecuteWorkflowInstructionResult>> ExecuteInstructionsAsync(IEnumerable<IWorkflowInstruction> instructions, CancellationToken cancellationToken = default)
    {
        var tasks = instructions.Select(x => ExecuteInstructionAsync(x, cancellationToken));
        return (await Task.WhenAll(tasks)).SelectMany(x => x);
    }

    public async Task<IEnumerable<DispatchWorkflowInstructionResult>> DispatchInstructionAsync(IWorkflowInstruction instruction, CancellationToken cancellationToken = default)
    {
        var handlers = _workflowExecutionInstructionHandlers.Where(x => x.GetSupportsInstruction(instruction)).ToList();
        var tasks = handlers.Select(x => x.DispatchInstructionAsync(instruction, cancellationToken).AsTask());
        var results = await Task.WhenAll(tasks);
        return results.Where(x => x != null).Select(x => x!).ToList();
    }

    public async Task<IEnumerable<DispatchWorkflowInstructionResult>> DispatchInstructionsAsync(IEnumerable<IWorkflowInstruction> instructions, CancellationToken cancellationToken = default)
    {
        var tasks = instructions.Select(x => DispatchInstructionAsync(x, cancellationToken));
        return (await Task.WhenAll(tasks)).SelectMany(x => x);
    }
}