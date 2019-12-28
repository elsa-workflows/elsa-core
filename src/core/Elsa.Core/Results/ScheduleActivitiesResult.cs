using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class ScheduleActivitiesResult : ActivityExecutionResult
    {
        public ScheduleActivitiesResult(IEnumerable<IActivity> activities, Variable input = null)
        {
            Activities = activities.ToList();
            Input = input;
        }
        
        public ICollection<IActivity> Activities { get; }
        public Variable Input { get; }
        
        protected override void Execute(IWorkflowRunner runner, WorkflowExecutionContext workflowContext)
        {
            workflowContext.ScheduleActivities(Activities, Input);
        }
    }
}