using System.Collections.Generic;

namespace Elsa.Activities.Conductor.Models
{
    public record EventModel(string EventName, ICollection<string>? Outcomes, object? Payload, string? WorkflowInstanceId, string? CorrelationId);
}