using Elsa.Models;

namespace Elsa.Services.Models
{
    public record StartableWorkflow(IWorkflowBlueprint WorkflowBlueprint, WorkflowInstance WorkflowInstance, string? ActivityId);
}