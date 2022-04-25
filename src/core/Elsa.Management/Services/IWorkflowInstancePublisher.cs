namespace Elsa.Management.Services;

public interface IWorkflowInstancePublisher
{
    Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default);
}