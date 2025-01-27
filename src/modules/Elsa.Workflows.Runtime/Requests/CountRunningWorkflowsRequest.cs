namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// Contains arguments to use for counting the number of workflow instances.
/// </summary>
public class CountRunningWorkflowsRequest
{
    /// <summary>
    /// The workflow definition ID to include in the query.
    /// </summary>
    public string? DefinitionId { get; set; }

    /// <summary>
    /// The workflow definition version to include in the query.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// The correlation ID to include in the query. 
    /// </summary>
    public string? CorrelationId { get; set; }
}