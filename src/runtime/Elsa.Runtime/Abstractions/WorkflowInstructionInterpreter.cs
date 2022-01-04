using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Abstractions;

public abstract class WorkflowInstructionInterpreter<TInstruction> : IWorkflowInstructionInterpreter where TInstruction: IWorkflowInstruction
{
    bool IWorkflowInstructionInterpreter.GetSupportsInstruction(IWorkflowInstruction instruction) => instruction is TInstruction;

    ValueTask<ExecuteWorkflowInstructionResult?> IWorkflowInstructionInterpreter.ExecuteInstructionAsync(IWorkflowInstruction instruction, CancellationToken cancellationToken) => ExecuteInstructionAsync((TInstruction)instruction, cancellationToken);
    ValueTask<DispatchWorkflowInstructionResult?> IWorkflowInstructionInterpreter.DispatchInstructionAsync(IWorkflowInstruction instruction, CancellationToken cancellationToken) => DispatchInstructionAsync((TInstruction)instruction, cancellationToken);

    protected virtual ValueTask<ExecuteWorkflowInstructionResult?> ExecuteInstructionAsync(TInstruction instruction, CancellationToken cancellationToken = default)
    {
        var result = ExecuteInstruction(instruction);
        return ValueTask.FromResult(result);
    }
        
    protected virtual ValueTask<DispatchWorkflowInstructionResult?> DispatchInstructionAsync(TInstruction instruction, CancellationToken cancellationToken = default)
    {
        var result = DispatchInstruction(instruction);
        return ValueTask.FromResult(result);
    }

    protected virtual ExecuteWorkflowInstructionResult? ExecuteInstruction(TInstruction instruction) => null;
    protected virtual DispatchWorkflowInstructionResult? DispatchInstruction(TInstruction instruction) => null;
}