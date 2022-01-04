using System.Collections.Generic;

namespace Elsa.Persistence.Entities;

public class WorkflowBookmark : Entity
{
    public string Name { get; init; } = default!;
    public string? Hash { get; set; }
    public string WorkflowDefinitionId { get; init; } = default!;
    public string WorkflowInstanceId { get; init; } = default!;
    public string CorrelationId { get; init; } = default!;
    public string ActivityId { get; init; } = default!;
    public string ActivityInstanceId { get; init; } = default!;
    public IDictionary<string, object?>? Data { get; set; }
    public string? CallbackMethodName { get; set; }
}