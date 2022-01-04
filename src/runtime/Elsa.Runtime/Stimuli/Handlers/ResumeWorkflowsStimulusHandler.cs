using Elsa.Mediator.Contracts;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Abstractions;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Instructions;

namespace Elsa.Runtime.Stimuli.Handlers;

public class ResumeWorkflowsStimulusHandler : StimulusHandler<StandardStimulus>
{
    private readonly IRequestSender _mediator;
    public ResumeWorkflowsStimulusHandler(IRequestSender mediator) => _mediator = mediator;

    protected override async ValueTask<IEnumerable<IWorkflowInstruction>> GetInstructionsAsync(StandardStimulus stimulus, CancellationToken cancellationToken = default)
    {
        var workflowBookmarks = (await _mediator.RequestAsync(new FindWorkflowBookmarks(stimulus.ActivityTypeName, stimulus.Hash), cancellationToken)).ToList();
        return workflowBookmarks.Select(x => new ResumeWorkflowInstruction(x));
    }
}