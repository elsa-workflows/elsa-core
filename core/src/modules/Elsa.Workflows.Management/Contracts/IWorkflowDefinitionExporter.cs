using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management;

/// <summary>
/// Exports workflow definitions as serialized JSON or ZIP archives.
/// </summary>
public interface IWorkflowDefinitionExporter
{
    /// <summary>
    /// Exports a single workflow definition as a JSON byte array, optionally including consuming workflows as a ZIP archive.
    /// </summary>
    /// <param name="definitionId">The definition ID.</param>
    /// <param name="versionOptions">The version options. Defaults to <see cref="VersionOptions.Latest"/>.</param>
    /// <param name="includeConsumingWorkflows">When true, includes all consuming workflow definitions in the export as a ZIP archive.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An export result containing the binary content and a suggested file name, or null if the definition was not found.</returns>
    Task<WorkflowDefinitionExportResult?> ExportAsync(string definitionId, VersionOptions? versionOptions = null, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports multiple workflow definitions as a ZIP archive.
    /// </summary>
    /// <param name="ids">A list of workflow definition version IDs.</param>
    /// <param name="includeConsumingWorkflows">When true, includes all consuming workflow definitions in the export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An export result containing the ZIP binary content and a suggested file name, or null if no definitions were found.</returns>
    Task<WorkflowDefinitionExportResult?> ExportManyAsync(ICollection<string> ids, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a single workflow definition entity to a JSON byte array (with $schema header).
    /// </summary>
    /// <param name="definition">The workflow definition entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The serialized JSON bytes and suggested file name.</returns>
    Task<WorkflowDefinitionExportResult> ExportDefinitionAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports multiple workflow definition entities as a ZIP archive.
    /// </summary>
    /// <param name="definitions">The workflow definitions to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ZIP archive bytes and suggested file name.</returns>
    Task<WorkflowDefinitionExportResult> ExportDefinitionsAsync(ICollection<WorkflowDefinition> definitions, CancellationToken cancellationToken = default);
}