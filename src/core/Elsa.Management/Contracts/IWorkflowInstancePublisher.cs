namespace Elsa.Management.Contracts;

public interface IWorkflowInstancePublisher
{
    Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default);
}