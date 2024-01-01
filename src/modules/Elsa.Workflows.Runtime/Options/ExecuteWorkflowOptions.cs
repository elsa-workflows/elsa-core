using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for executing workflows.
/// </summary>
public class ExecuteWorkflowOptions
{
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public CancellationTokens CancellationTokens { get; set; }
}