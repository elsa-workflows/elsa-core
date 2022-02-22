using Elsa.Models;

namespace Elsa.Persistence.Entities;

public class WorkflowBookmark : Entity
{
    public string Name { get; init; } = default!;
    public string? Hash { get; set; }
    public string? Payload { get; set; }
    public string WorkflowDefinitionId { get; init; } = default!;
    public string WorkflowInstanceId { get; init; } = default!;
    public string CorrelationId { get; init; } = default!;
    public string ActivityId { get; init; } = default!;
    public string ActivityInstanceId { get; init; } = default!;
    public string? CallbackMethodName { get; set; }

    public Bookmark ToBookmark() => new(Id, Name, Hash, Payload, ActivityId, ActivityInstanceId, CallbackMethodName);

    public static WorkflowBookmark FromBookmark(Bookmark bookmark, WorkflowInstance workflowInstance) =>
        new()
        {
            Id = bookmark.Id,
            WorkflowDefinitionId = workflowInstance.DefinitionId,
            WorkflowInstanceId = workflowInstance.Id,
            CorrelationId = workflowInstance.CorrelationId,
            Hash = bookmark.Hash,
            Payload = bookmark.Payload,
            Name = bookmark.Name,
            ActivityId = bookmark.ActivityId,
            ActivityInstanceId = bookmark.ActivityInstanceId,
            CallbackMethodName = bookmark.CallbackMethodName
        };
}