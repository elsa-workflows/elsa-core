namespace Elsa.Workflows.Models;

public record WorkflowIdentity(string DefinitionId, int Version, string Id, string? TenantId = null)
{
    public static WorkflowIdentity VersionOne => new("1", 1, "1", null);
}