namespace Elsa.Workflows.Core.Models;

public record WorkflowIdentity(string DefinitionId, int Version, string Id)
{
    public static WorkflowIdentity VersionOne => new("1", 1, "1");
}