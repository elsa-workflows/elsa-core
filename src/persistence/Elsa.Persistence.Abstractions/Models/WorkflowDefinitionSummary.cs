using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Models;

public record WorkflowDefinitionSummary(string Id, string DefinitionId, string? Name, int Version, bool IsLatest, bool IsPublished, DateTimeOffset CreatedAt)
{
    public static WorkflowDefinitionSummary FromDefinition(WorkflowDefinition workflowDefinition) => new(
        workflowDefinition.Id,
        workflowDefinition.DefinitionId,
        workflowDefinition.Name,
        workflowDefinition.Version,
        workflowDefinition.IsLatest,
        workflowDefinition.IsPublished,
        workflowDefinition.CreatedAt
    );
}