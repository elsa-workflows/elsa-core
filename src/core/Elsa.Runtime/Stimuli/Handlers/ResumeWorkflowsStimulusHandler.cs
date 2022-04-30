using Elsa.Persistence.Services;
using Elsa.Runtime.Abstractions;
using Elsa.Runtime.Interpreters;
using Elsa.Runtime.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.Runtime.Stimuli.Handlers;

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