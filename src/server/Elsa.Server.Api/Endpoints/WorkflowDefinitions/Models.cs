using Elsa.Models;
using NodaTime;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions;

public record WorkflowDefinitionSummaryModel(
    string Id,
    string DefinitionId,
    string? TenantId,
    string? Name,
    string? DisplayName,
    string? Description,
    int Version,
    bool IsSingleton,
    WorkflowPersistenceBehavior PersistenceBehavior,
    bool IsPublished,
    bool IsLatest,
    Variables CustomAttributes,
    Instant CreatedAt);
    
public record WorkflowDefinitionVersionModel(string Id, string DefinitionId, int Version, bool IsLatest, bool IsPublished, Instant CreatedAt);