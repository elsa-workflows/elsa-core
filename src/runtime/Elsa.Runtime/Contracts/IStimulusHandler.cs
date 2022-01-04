namespace Elsa.Runtime.Contracts;

public interface IStimulusHandler
{
    bool GetSupportsStimulus(IStimulus stimulus);
    ValueTask<IEnumerable<IWorkflowInstruction>> GetInstructionsAsync(IStimulus stimulus, CancellationToken cancellationToken = default);
}