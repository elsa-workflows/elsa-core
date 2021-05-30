using System.Collections.Generic;

namespace Elsa.Activities.Conductor.Models
{
    public record TaskResultModel(string TaskName, ICollection<string>? Outcomes, object? Payload, string? WorkflowInstanceId, string? CorrelationId);
}