namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents an interface responsible for starting workflows.
/// Provides a method to start a workflow based on the provided request.
/// </summary>
public interface IWorkflowStarter
{
    public Task<StartWorkflowResponse> StartWorkflowAsync(StartWorkflowRequest request, CancellationToken cancellationToken = default);
}