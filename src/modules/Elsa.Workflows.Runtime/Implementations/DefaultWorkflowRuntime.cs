using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class DefaultWorkflowRuntime : IWorkflowRuntime
{
    public async Task<StartWorkflowResult> StartWorkflowAsync(string definitionId, StartWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<ResumeWorkflowResult> ResumeWorkflowAsync(string instanceId, string bookmarkId, ResumeWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(object bookmarkPayload, TriggerWorkflowsOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}