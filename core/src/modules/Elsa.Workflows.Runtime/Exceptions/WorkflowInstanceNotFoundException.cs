namespace Elsa.Workflows.Runtime.Exceptions;

public class WorkflowInstanceNotFoundException(string message, string instanceId) : Exception(message)
{
    public string InstanceId { get; } = instanceId;
}