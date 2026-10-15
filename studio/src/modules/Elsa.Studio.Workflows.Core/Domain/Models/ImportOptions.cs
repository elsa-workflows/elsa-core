using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents configuration options that control workflow definition import operations.
/// </summary>
public class ImportOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed payload size for imports in bytes.
    /// </summary>
    public int MaxAllowedSize { get; set; } = 1024 * 1024 * 10; // 10 MB

    /// <summary>
    /// Gets or sets the target workflow definition identifier for the import.
    /// </summary>
    public string? DefinitionId { get; set; }

    /// <summary>
    /// Gets or sets a callback that is invoked after a workflow definition has been imported.
    /// </summary>
    public Func<WorkflowDefinition, Task>? ImportedCallback { get; set; }

    /// <summary>
    /// Gets or sets a callback that is invoked when an error occurs during import.
    /// </summary>
    public Func<Exception, Task>? ErrorCallback { get; set; }
}
