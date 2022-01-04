using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Abstractions;

public abstract class StimulusHandler<TStimulus> : IStimulusHandler where TStimulus:IStimulus
{
    bool IStimulusHandler.GetSupportsStimulus(IStimulus stimulus) => stimulus is TStimulus;
    ValueTask<IEnumerable<IWorkflowInstruction>> IStimulusHandler.GetInstructionsAsync(IStimulus stimulus, CancellationToken cancellationToken) => GetInstructionsAsync((TStimulus)stimulus, cancellationToken);

    protected virtual ValueTask<IEnumerable<IWorkflowInstruction>> GetInstructionsAsync(TStimulus stimulus, CancellationToken cancellationToken = default)
    {
        var instruction = GetInstructions(stimulus);
        return ValueTask.FromResult(instruction);
    }

    protected virtual IEnumerable<IWorkflowInstruction> GetInstructions(TStimulus stimulus) => ArraySegment<IWorkflowInstruction>.Empty;
}