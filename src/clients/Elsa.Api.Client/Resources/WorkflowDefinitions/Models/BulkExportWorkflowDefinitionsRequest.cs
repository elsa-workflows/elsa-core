namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// A request to bulk export workflow definitions.
/// </summary>
/// <param name="Ids">The version IDs of the workflow definitions to export.</param>
public record BulkExportWorkflowDefinitionsRequest(string[] Ids);