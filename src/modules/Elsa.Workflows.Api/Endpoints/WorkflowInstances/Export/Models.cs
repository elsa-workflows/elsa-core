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
}