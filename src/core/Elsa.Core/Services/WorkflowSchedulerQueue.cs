using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class WorkflowSchedulerQueue : IWorkflowSchedulerQueue
    {
        private readonly IDictionary<(string WorkflowDefinitionId, string ActivityId), (IWorkflowBlueprint Workflow, IActivityBlueprint Activity, object? Input, string? CorrelationId)> _nextWorkflowInstances;

        public WorkflowSchedulerQueue() =>
            _nextWorkflowInstances = new Dictionary<(string WorkflowDefinitionId, string ActivityId), (IWorkflowBlueprint Workflow, IActivityBlueprint Activity, object? Input, string? CorrelationId)>();

        public void Enqueue(IWorkflowBlueprint workflowBlueprint, IActivityBlueprint activity, object? input, string? correlationId)
            => _nextWorkflowInstances[(workflowBlueprint.Id, activity.Id)] = (workflowBlueprint, activity, input, correlationId);

        public (IWorkflowBlueprint Workflow, IActivityBlueprint Activity, object? Input, string? CorrelationId)? Dequeue(string workflowDefinitionId, string activityId)
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