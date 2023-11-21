namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Export;

/// <summary>
/// Exports the specified workflow definition as JSON download.
/// Either specify the <see cref="Id"/> or the <see cref="Ids"/> of the workflow instances to export.
/// </summary>
internal class Request
{
    /// <summary>
    /// The workflow instance ID.
    /// </summary>
    public string? Id { get; set; } = default!;
    
    /// <summary>
    /// A list of workflow instance IDs.
    /// </summary>
    public ICollection<string> Ids { get; set; } = default!;

    /// <summary>
    /// A flag indicating whether to include the bookmarks in the export.
    /// </summary>
    public bool IncludeBookmarks { get; set; }
    
    /// <summary>
    /// A flag indicating whether to include the activity execution log in the export.
    /// </summary>
    public bool IncludeActivityExecutionLog { get; set; }
    
    /// <summary>
    /// A flag indicating whether to include the workflow execution log in the export.
    /// </summary>
    public bool IncludeWorkflowExecutionLog { get; set; }
}