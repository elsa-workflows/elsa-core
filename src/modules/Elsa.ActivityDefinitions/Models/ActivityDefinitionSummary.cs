using Elsa.ActivityDefinitions.Entities;

namespace Elsa.ActivityDefinitions.Models;

public record ActivityDefinitionSummary(string Id, string DefinitionId, string? Name, string? Description, int? Version, bool IsLatest, bool IsPublished, DateTimeOffset CreatedAt)
{
    public static ActivityDefinitionSummary FromDefinition(ActivityDefinition workflowDefinition) => new(
        workflowDefinition.Id,
        workflowDefinition.DefinitionId,
        workflowDefinition.Name,
        workflowDefinition.Description,
        workflowDefinition.Version,
        workflowDefinition.IsLatest,
        workflowDefinition.IsPublished,
        workflowDefinition.CreatedAt
    );
}