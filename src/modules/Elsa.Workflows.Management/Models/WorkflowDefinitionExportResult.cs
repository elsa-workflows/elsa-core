namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents the result of exporting one or more workflow definitions.
/// </summary>
/// <param name="Data">The binary content (JSON or ZIP).</param>
/// <param name="FileName">The suggested file name.</param>
public record WorkflowDefinitionExportResult(byte[] Data, string FileName);