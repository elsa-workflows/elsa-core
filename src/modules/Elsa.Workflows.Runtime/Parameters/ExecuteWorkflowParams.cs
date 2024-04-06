using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Parameters;

/// <summary>
/// Options for executing workflows.
/// </summary>
public class ExecuteWorkflowParams
{
    public string? CorrelationId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public ActivityHandle ActivityHandle { get; set; }
    public CancellationToken CancellationToken { get; set; }
}