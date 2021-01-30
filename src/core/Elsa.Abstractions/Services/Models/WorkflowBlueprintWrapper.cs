using System.Collections.Generic;
using System.Threading;

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
                var activityExecutionContext = new ActivityExecutionContext(_workflowExecutionContext.ServiceProvider, _workflowExecutionContext, activity, null, false, CancellationToken.None);
                yield return new ActivityBlueprintWrapper(activityExecutionContext);
            }
        }
    }
}