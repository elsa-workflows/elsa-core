namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// A request to bulk export workflow instances.
/// </summary>
/// <param name="Ids">The version IDs of the workflow instances to export.</param>
public record BulkExportWorkflowInstancesRequest(string[] Ids);
