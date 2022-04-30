namespace Elsa.Runtime.Services;

public interface IStimulusInterpreter
{
    Task<IEnumerable<IWorkflowInstruction>> GetExecutionInstructionsAsync(IStimulus stimulus, CancellationToken cancellationToken = default);
}