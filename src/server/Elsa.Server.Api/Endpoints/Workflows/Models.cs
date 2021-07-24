using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Bookmarks;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Endpoints.Workflows
{
    public record ExecuteWorkflowDefinitionRequestModel(string? ActivityId, string? CorrelationId, string? ContextId, object? Input);

    public record ExecuteWorkflowDefinitionResponseModel(bool Executed, string? ActivityId, WorkflowInstance? WorkflowInstance);

    public record DispatchWorkflowDefinitionRequestModel(string? ActivityId, string? CorrelationId, string? ContextId, object? Input);

    public record DispatchWorkflowDefinitionResponseModel(string WorkflowInstanceId, string? ActivityId);
    
    public record TriggeredWorkflowModel(string WorkflowInstanceId, string? ActivityId);

    public record DispatchTriggerWorkflowsRequestModel(string ActivityType, IBookmark? Bookmark, IBookmark? Trigger, string? CorrelationId, string? WorkflowInstanceId, string? ContextId, object? Input);

    public record DispatchTriggerWorkflowsResponseModel(ICollection<CollectedWorkflow> PendingWorkflows);
    public record TriggerWorkflowsRequestModel(string ActivityType, IBookmark? Bookmark, string? CorrelationId, string? WorkflowInstanceId, string? ContextId, object? Input, bool Dispatch);

    public record TriggerWorkflowsResponseModel(ICollection<TriggeredWorkflowModel> TriggeredWorkflows);
}