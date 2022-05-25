using Elsa.Persistence.Services;
using Elsa.Workflows.Runtime.Abstractions;
using Elsa.Workflows.Runtime.Interpreters;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Stimuli.Handlers;

public class TriggerWorkflowsStimulusHandler : StimulusHandler<StandardStimulus>
{
    private readonly IWorkflowTriggerStore _workflowTriggerStore;

    public TriggerWorkflowsStimulusHandler(IWorkflowTriggerStore workflowTriggerStore)
    {
        _workflowTriggerStore = workflowTriggerStore;
    }

    protected override async ValueTask<IEnumerable<IWorkflowInstruction>> GetInstructionsAsync(StandardStimulus stimulus, CancellationToken cancellationToken = default)
    {
        var workflowTriggers = (await _workflowTriggerStore.FindManyByNameAsync(stimulus.ActivityTypeName, stimulus.Hash, cancellationToken)).ToList();
        return workflowTriggers.Select(x => new TriggerWorkflowInstruction(x, stimulus.Input, stimulus.CorrelationId));
    }
}