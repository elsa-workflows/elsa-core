using Elsa.Models;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    public record WorkflowDefinitionSummaryModel(
        string Id,
        string DefinitionId,
        string? Name,
        string? DisplayName,
        string? Description,
        int Version,
        bool IsSingleton,
        WorkflowPersistenceBehavior PersistenceBehavior,
        bool IsPublished,
        bool IsLatest);
}