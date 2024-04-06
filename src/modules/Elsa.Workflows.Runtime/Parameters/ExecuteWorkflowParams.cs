namespace Elsa.Workflows.Runtime.Parameters;

/// <summary>
/// Options for executing workflows.
/// </summary>
public class ExecuteWorkflowParams
{
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public CancellationToken CancellationToken { get; set; }
}