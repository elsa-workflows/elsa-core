namespace Elsa.Workflows.Management.Exceptions;

public class WorkflowMaterializerNotFoundException(string materializerName, string message = "Materializer not found. The materializer may be disabled or not registered") : Exception(message)
{
    public string MaterializerName { get; } = materializerName;
}