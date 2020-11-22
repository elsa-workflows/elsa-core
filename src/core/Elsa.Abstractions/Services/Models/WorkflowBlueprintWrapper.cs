using System.Collections.Generic;

namespace Elsa.Services.Models
{
    public class WorkflowBlueprintWrapper : IWorkflowBlueprintWrapper
    {
        private readonly WorkflowExecutionContext _workflowExecutionContext;

        public WorkflowBlueprintWrapper(IWorkflowBlueprint workflowBlueprint, WorkflowExecutionContext workflowExecutionContext)
        {
            _workflowExecutionContext = workflowExecutionContext;
            WorkflowBlueprint = workflowBlueprint;
            Activities = GetActivityBlueprintWrappers();
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }

        public IEnumerable<IActivityBlueprintWrapper> Activities { get; }

        private IEnumerable<IActivityBlueprintWrapper> GetActivityBlueprintWrappers()
        {
            var activities = WorkflowBlueprint.Activities;

            foreach (var activity in activities)
            {
                var activityExecutionContext = new ActivityExecutionContext(_workflowExecutionContext, _workflowExecutionContext.ServiceProvider, activity);
                yield return new ActivityBlueprintWrapper(activityExecutionContext);
            }
        }
    }
}