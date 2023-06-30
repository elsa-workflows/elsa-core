namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.IsNameUnique;

internal class Request
{
    /// <summary>
    /// The name to check.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// Optional. If provided, the name will be checked against all workflow definitions except the one with the specified ID.
    /// </summary>
    public string? DefinitionId { get; set; }
}

/// <summary>
/// Represents a response indicating whether a workflow definition name is unique.
/// </summary>
/// <param name="IsUnique">A value indicating whether the name is unique.</param>
internal record Response(bool IsUnique);
