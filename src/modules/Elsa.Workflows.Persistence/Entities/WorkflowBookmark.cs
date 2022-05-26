using Elsa.Persistence.Common.Entities;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Persistence.Entities;

public class WorkflowBookmark : Entity
{
    public string Name { get; init; } = default!;
    public string? Hash { get; set; }
    public string? Data { get; set; }
    public string WorkflowDefinitionId { get; init; } = default!;
    public string WorkflowInstanceId { get; init; } = default!;
    public string? CorrelationId { get; init; }
    public string ActivityId { get; init; } = default!;
    public string ActivityInstanceId { get; init; } = default!;
    public string? CallbackMethodName { get; set; }

    public Bookmark ToBookmark() => new(Id, Name, Hash, Data, ActivityId, ActivityInstanceId, CallbackMethodName);

    public static WorkflowBookmark FromBookmark(Bookmark bookmark, WorkflowInstance workflowInstance) =>
        new()
        {
            Id = bookmark.Id,
            WorkflowDefinitionId = workflowInstance.DefinitionId,
            WorkflowInstanceId = workflowInstance.Id,
            CorrelationId = workflowInstance.CorrelationId,
            Hash = bookmark.Hash,
            Data = bookmark.Data,
            Name = bookmark.Name,
            ActivityId = bookmark.ActivityId,
            ActivityInstanceId = bookmark.ActivityInstanceId,
            CallbackMethodName = bookmark.CallbackMethodName
        };
}