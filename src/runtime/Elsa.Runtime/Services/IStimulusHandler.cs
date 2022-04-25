namespace Elsa.Runtime.Services;

public interface IStimulusHandler
{
    bool GetSupportsStimulus(IStimulus stimulus);
    ValueTask<IEnumerable<IWorkflowInstruction>> GetInstructionsAsync(IStimulus stimulus, CancellationToken cancellationToken = default);
}