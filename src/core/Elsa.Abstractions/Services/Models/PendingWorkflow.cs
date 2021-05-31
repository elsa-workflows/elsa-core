namespace Elsa.Services.Models
{
    public record PendingWorkflow(string WorkflowInstanceId, string? ActivityId);
}