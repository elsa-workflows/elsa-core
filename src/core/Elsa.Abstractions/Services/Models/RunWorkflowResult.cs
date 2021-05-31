using Elsa.Models;

namespace Elsa.Services.Models
{
    public record RunWorkflowResult(WorkflowInstance? WorkflowInstance, string? ActivityId, bool Executed);
}