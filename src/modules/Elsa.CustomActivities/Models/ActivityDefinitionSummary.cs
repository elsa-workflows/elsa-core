using Elsa.CustomActivities.Entities;

namespace Elsa.CustomActivities.Models;

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