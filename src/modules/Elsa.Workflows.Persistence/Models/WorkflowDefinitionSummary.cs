using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Persistence.Models;

public record WorkflowDefinitionSummary(string Id, string DefinitionId, string? Name, string? Description, int? Version, bool IsLatest, bool IsPublished, string MaterializerName, DateTimeOffset CreatedAt)
{
    public static WorkflowDefinitionSummary FromDefinition(WorkflowDefinition workflowDefinition) => new(
        workflowDefinition.Id,
        workflowDefinition.DefinitionId,
        workflowDefinition.Name,
        workflowDefinition.Description,
        workflowDefinition.Version,
        workflowDefinition.IsLatest,
        workflowDefinition.IsPublished,
        workflowDefinition.MaterializerName,
        workflowDefinition.CreatedAt
    );
}