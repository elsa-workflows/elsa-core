using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Abstractions;
using Elsa.Workflows.Runtime.Interpreters;
using Elsa.Workflows.Runtime.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.Stimuli.Handlers;

public class ResumeWorkflowsStimulusHandler : StimulusHandler<StandardStimulus>
{
    private readonly IWorkflowBookmarkStore _bookmarkStore;
    public ResumeWorkflowsStimulusHandler(IWorkflowBookmarkStore bookmarkStore) => _bookmarkStore = bookmarkStore;

    protected override async ValueTask<IEnumerable<IWorkflowInstruction>> GetInstructionsAsync(StandardStimulus stimulus, CancellationToken cancellationToken = default)
    {
        var workflowBookmarks = await _bookmarkStore.FindManyAsync(stimulus.ActivityTypeName, stimulus.Hash, cancellationToken).ToList();
        return workflowBookmarks.Select(x => new ResumeWorkflowInstruction(x, stimulus.Input, stimulus.CorrelationId));
    }
}