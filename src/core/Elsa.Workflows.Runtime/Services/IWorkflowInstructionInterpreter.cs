using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

public interface IWorkflowInstructionInterpreter
{
    bool GetSupportsInstruction(IWorkflowInstruction instruction);
    ValueTask<ExecuteWorkflowInstructionResult?> ExecuteInstructionAsync(IWorkflowInstruction instruction, CancellationToken cancellationToken = default);
    ValueTask<DispatchWorkflowInstructionResult?> DispatchInstructionAsync(IWorkflowInstruction instruction, CancellationToken cancellationToken = default);
}