using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowSchedulerQueue
    {
        void Enqueue(Workflow workflow, IActivity activity, object? input, string? correlationId);
        (Workflow Workflow, IActivity Activity, object? Input, string? CorrelationId)? Dequeue(string workflowDefinitionId, string activityId);
    }
}