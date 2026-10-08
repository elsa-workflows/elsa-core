namespace Elsa.Workflows.Management.Options;

/// <summary>
/// Options for configuring the workflow reference graph builder behavior.
/// </summary>
public class WorkflowReferenceGraphOptions
{
    /// <summary>
    /// The maximum depth to traverse when building the reference graph.
    /// A value of 0 means no limit.
    /// Default is 100.
    /// </summary>
    public int MaxDepth { get; set; } = 100;

    /// <summary>
    /// The maximum number of workflow definitions to include in the graph.
    /// A value of 0 means no limit.
    /// Default is 1000.
    /// </summary>
    public int MaxDefinitions { get; set; } = 1000;
}

