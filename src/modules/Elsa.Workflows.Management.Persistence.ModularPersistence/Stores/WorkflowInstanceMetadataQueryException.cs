namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Stores;

public sealed class WorkflowInstanceMetadataQueryException : InvalidOperationException
{
    public WorkflowInstanceMetadataQueryException(string message) : base(message)
    {
    }
}
