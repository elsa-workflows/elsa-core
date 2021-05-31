using System.Collections.Generic;
using Elsa.Bookmarks;
using Elsa.Models;
using Elsa.Services.Models;

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

    public record ExecuteWorkflowsRequest(string ActivityType, IBookmark? Bookmark, IBookmark? Trigger, string? CorrelationId, string? WorkflowInstanceId, string? ContextId, object? Input);

    public record ExecuteWorkflowsResponse(ICollection<StartedWorkflow> StartedWorkflows);

    public record DispatchWorkflowsRequest(string ActivityType, IBookmark? Bookmark, IBookmark? Trigger, string? CorrelationId, string? WorkflowInstanceId, string? ContextId, object? Input);

    public record DispatchWorkflowsResponse(ICollection<PendingWorkflow> PendingWorkflows);
}