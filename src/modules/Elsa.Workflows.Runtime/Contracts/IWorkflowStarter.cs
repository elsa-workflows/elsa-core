namespace Elsa.Workflows.Runtime;

public interface IWorkflowStarter
{
    public Task<StartWorkflowResponse> StartWorkflowAsync(StartWorkflowRequest request, CancellationToken cancellationToken = default);
}