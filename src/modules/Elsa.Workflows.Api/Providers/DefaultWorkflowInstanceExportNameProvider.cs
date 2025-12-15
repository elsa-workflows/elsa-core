using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Api;

/// <summary>
/// Provides a default implementation for generating export file names for workflow instances.
/// </summary>
public class DefaultWorkflowInstanceExportNameProvider : IWorkflowInstanceExportNameProvider
{
    public Task<string> GetFileNameAsync(WorkflowInstance instance, object model, CancellationToken cancellationToken = default)
    {
        var fileName = $"workflow-instance-{instance.Id.ToLowerInvariant()}.json";
        return Task.FromResult(fileName);
    }
}
