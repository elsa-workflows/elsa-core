using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowSchedulerQueue
    {
        void Enqueue(WorkflowBlueprint workflowBlueprint, IActivity activity, object? input, string? correlationId);
        (WorkflowBlueprint Workflow, IActivity Activity, object? Input, string? CorrelationId)? Dequeue(string workflowDefinitionId, string activityId);
    }
}