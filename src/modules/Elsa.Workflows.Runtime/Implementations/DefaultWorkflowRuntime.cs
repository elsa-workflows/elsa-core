using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class DefaultWorkflowRuntime : IWorkflowRuntime
{
    public async Task StartWorkflowAsync(string definitionId, RunWorkflowOptions options, CancellationToken cancellationToken = default)
    {
    }

    public async Task ResumeWorkflowAsync(string definitionId, string instanceId, Bookmark bookmark, ResumeWorkflowOptions options, CancellationToken cancellationToken = default)
    {
    }
}