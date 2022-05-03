using Elsa.Runtime.Models;

namespace Elsa.Runtime.Services;

public interface IWorkflowInstructionScheduler
{
    Task<IEnumerable<ExecuteWorkflowInstructionResult?>> ScheduleInstructionAsync(IWorkflowInstruction instruction, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExecuteWorkflowInstructionResult?>> ScheduleInstructionsAsync(IEnumerable<IWorkflowInstruction> instructions, CancellationToken cancellationToken = default);
}