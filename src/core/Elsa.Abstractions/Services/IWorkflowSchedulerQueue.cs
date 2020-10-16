using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowSchedulerQueue
    {
        void Enqueue(IWorkflowBlueprint workflowBlueprint, IActivityBlueprint activity, object? input, string? correlationId);
        (IWorkflowBlueprint Workflow, IActivityBlueprint Activity, object? Input, string? CorrelationId)? Dequeue(string workflowDefinitionId, string activityId);
    }
}