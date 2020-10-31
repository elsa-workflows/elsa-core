using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowSchedulerQueue
    {
        void Enqueue(IWorkflowBlueprint workflowBlueprint, IActivityBlueprint activity, object? input, string? correlationId, string? contextId);
        (IWorkflowBlueprint Workflow, IActivityBlueprint Activity, object? Input, string? CorrelationId, string? ContextId)? Dequeue(string workflowDefinitionId, string activityId);
    }
}