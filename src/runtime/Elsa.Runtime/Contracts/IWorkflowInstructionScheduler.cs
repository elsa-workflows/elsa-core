namespace Elsa.Runtime.Contracts;

public interface IWorkflowInstructionScheduler
{
    Task<IEnumerable<ExecuteWorkflowInstructionResult?>> ScheduleInstructionAsync(IWorkflowInstruction instruction, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExecuteWorkflowInstructionResult?>> ScheduleInstructionsAsync(IEnumerable<IWorkflowInstruction> instructions, CancellationToken cancellationToken = default);
}