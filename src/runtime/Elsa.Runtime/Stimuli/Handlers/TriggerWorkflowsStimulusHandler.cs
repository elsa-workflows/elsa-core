using Elsa.Mediator.Contracts;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Abstractions;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Instructions;

namespace Elsa.Runtime.Stimuli.Handlers;

public class TriggerWorkflowsStimulusHandler : StimulusHandler<StandardStimulus>
{
    private readonly IRequestSender _mediator;
    public TriggerWorkflowsStimulusHandler(IRequestSender mediator) => _mediator = mediator;

    protected override async ValueTask<IEnumerable<IWorkflowInstruction>> GetInstructionsAsync(StandardStimulus stimulus, CancellationToken cancellationToken = default)
    {
        var workflowTriggers = (await _mediator.RequestAsync(new FindWorkflowTriggers(stimulus.ActivityTypeName, stimulus.Hash), cancellationToken)).ToList();
        return workflowTriggers.Select(x => new TriggerWorkflowInstruction(x));
    }
}