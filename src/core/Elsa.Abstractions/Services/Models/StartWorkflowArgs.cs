using Elsa.Models;

namespace Elsa.Services.Models
{
    public record StartWorkflowArgs(string? ActivityId = default, WorkflowInput? Input = default, string? CorrelationId = default, string? ContextId = default);
}