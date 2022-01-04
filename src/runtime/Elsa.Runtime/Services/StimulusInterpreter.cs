using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Services;

public class StimulusInterpreter : IStimulusInterpreter
{
    private readonly IEnumerable<IStimulusHandler> _workflowExecutionInstructionProviders;

    public StimulusInterpreter(IEnumerable<IStimulusHandler> workflowExecutionInstructionProviders)
    {
        _workflowExecutionInstructionProviders = workflowExecutionInstructionProviders;
    }

    public async Task<IEnumerable<IWorkflowInstruction>> GetExecutionInstructionsAsync(IStimulus stimulus, CancellationToken cancellationToken = default)
    {
        var providers = _workflowExecutionInstructionProviders.Where(x => x.GetSupportsStimulus(stimulus)).ToList();
        var tasks = providers.Select(x => x.GetInstructionsAsync(stimulus, cancellationToken).AsTask());
        return (await Task.WhenAll(tasks)).SelectMany(x => x);
    }
}