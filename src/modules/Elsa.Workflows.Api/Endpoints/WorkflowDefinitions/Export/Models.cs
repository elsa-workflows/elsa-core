namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Export;

/// <summary>
/// Exports the specified workflow definition as JSON download.
/// Either specify the <see cref="DefinitionId"/> and optionally the <see cref="VersionOptions"/> or the <see cref="Ids"/> of the workflow definition versions to export.
/// </summary>
internal class Request
{
    /// <summary>
    /// The workflow definition ID.
    /// </summary>
    public string? DefinitionId { get; set; } = default!;
    
    /// <summary>
    /// The version options.
    /// </summary>
    public string? VersionOptions { get; set; }
    
    /// <summary>
    /// A list of workflow definition version IDs.
    /// </summary>
    public ICollection<string>? Ids { get; set; } = default!;
    
    /// <summary>
    /// Whether to include consuming workflow definitions in the export.
    /// </summary>
    public bool IncludeConsumers { get; set; }
}