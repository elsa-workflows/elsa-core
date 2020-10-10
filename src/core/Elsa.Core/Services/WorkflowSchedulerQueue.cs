using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class WorkflowSchedulerQueue : IWorkflowSchedulerQueue
    {
        private readonly IDictionary<(string WorkflowDefinitionId, string ActivityId), (Workflow Workflow, IActivity Activity, object? Input, string? CorrelationId)> _nextWorkflowInstances;

        public WorkflowSchedulerQueue() =>
            _nextWorkflowInstances = new Dictionary<(string WorkflowDefinitionId, string ActivityId), (Workflow Workflow, IActivity Activity, object? Input, string? CorrelationId)>();

        public void Enqueue(Workflow workflow, IActivity activity, object? input, string? correlationId)
            => _nextWorkflowInstances[(workflow.WorkflowDefinitionId, activity.Id)] = (workflow, activity, input, correlationId);

        public (Workflow Workflow, IActivity Activity, object? Input, string? CorrelationId)? Dequeue(string workflowDefinitionId, string activityId)
        {
            var key = (workflowDefinitionId, activityId);
            if(!_nextWorkflowInstances.ContainsKey(key))
                return default;
            
            var entry = _nextWorkflowInstances[key];
            _nextWorkflowInstances.Remove(key);
            
            return entry;
        }
    }
}