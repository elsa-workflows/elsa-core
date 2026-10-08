using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// A service that imports workflow definitions.
/// </summary>
public interface IWorkflowDefinitionImporter
{
    /// <summary>
    /// Imports a set of files containing workflow definitions.
    /// </summary>
    Task<IEnumerable<WorkflowImportResult>> ImportFilesAsync(IReadOnlyList<IBrowserFile> files, ImportOptions? options = null);

    /// <summary>
    /// Imports workflow definitions from a ZIP file stream.
    /// </summary>
    Task<IList<WorkflowImportResult>> ImportZipFileAsync(Stream stream, ImportOptions? options);

    /// <summary>
    /// Imports a workflow definition from a given stream and file name.
    /// </summary>
    Task<WorkflowImportResult> ImportFromStreamAsync(string fileName, Stream stream, ImportOptions? options = null);

    /// <summary>
    /// Imports a workflow definition from a JSON file.
    /// </summary>
    Task<WorkflowImportResult> ImportJsonAsync(string fileName, string json, ImportOptions? options = null);
}