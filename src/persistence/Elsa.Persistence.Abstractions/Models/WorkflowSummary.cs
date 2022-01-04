using System;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Models;

public record WorkflowSummary(string Id, string DefinitionId, string? Name, int Version, bool IsLatest, bool IsPublished, DateTime CreatedAt)
{
    public static WorkflowSummary FromDefinition(WorkflowDefinition workflowDefinition) => new(
        workflowDefinition.Id,
        workflowDefinition.DefinitionId,
        workflowDefinition.Name,
        workflowDefinition.Version,
        workflowDefinition.IsLatest,
        workflowDefinition.IsPublished,
        workflowDefinition.CreatedAt
    );
}