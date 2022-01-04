namespace Elsa.Runtime.Contracts;

public interface IStimulusInterpreter
{
    Task<IEnumerable<IWorkflowInstruction>> GetExecutionInstructionsAsync(IStimulus stimulus, CancellationToken cancellationToken = default);
}