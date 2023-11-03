namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents metadata about a workflow.
/// </summary>
/// <param name="Name">The name of the workflow.</param>
/// <param name="Description">The description of the workflow.</param>
/// <param name="CreatedAt">The date and time the workflow was created.</param>
/// <param name="ToolVersion">The version of the tool that created the workflow.</param>
/// <param name="TenantId">The id of the tenant that own the workflow.</param>
public record WorkflowMetadata(string? Name = default, string? Description = default, DateTimeOffset CreatedAt = default, Version? ToolVersion = default, string? TenantId = default)
{
}