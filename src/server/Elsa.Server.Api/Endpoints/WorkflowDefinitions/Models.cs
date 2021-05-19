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

    public record ExecuteWorkflowDefinitionRequest(string? ActivityId, string? CorrelationId, string? ContextId, object? Input);

    public record ExecuteWorkflowDefinitionResponse(bool Executed, string? ActivityId, WorkflowInstance? WorkflowInstance);

    public record DispatchWorkflowDefinitionRequest(string? ActivityId, string? CorrelationId, string? ContextId, object? Input);

    public record DispatchWorkflowDefinitionResponse(string WorkflowInstanceId, string? ActivityId);

    public record ExecuteWorkflowInstanceRequest(string? ActivityId, object? Input);

    public record ExecuteWorkflowInstanceResponse(bool Executed, string? ActivityId, WorkflowInstance? WorkflowInstance);

    public record DispatchWorkflowInstanceRequest(string? ActivityId, object? Input);

    public record DispatchWorkflowInstanceResponse();
}