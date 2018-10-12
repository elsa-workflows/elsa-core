using System.Collections.Generic;
using System.Linq;
using Flowsharp.Models;

namespace Flowsharp.ActivityResults
{
    /// <summary>
    /// An activity execution result that sets the next outcomes to execute.
    /// </summary>
    public class OutcomeResult : ActivityExecutionResult
    {
        private readonly IEnumerable<string> outcomeNames;

        public OutcomeResult(IEnumerable<string> names)
        {
            outcomeNames = names.ToList();
        }

        protected override void Execute(WorkflowExecutionContext workflowContext)
        {
            var workflowType = workflowContext.WorkflowType;
            var currentActivity = workflowContext.CurrentExecutingActivity;
            
            foreach (var outcome in outcomeNames)
            {
                // Look for next activity in the graph.
                var transition = workflowType.Transitions.FirstOrDefault(x => x.From.ActivityId == currentActivity.ActivityType.Id && x.From.OutcomeName == outcome);

                if (transition != null)
                {
                    var destinationActivity = workflowContext.Activities.Values.Single(x => x.ActivityType.Id == transition.To.ActivityId);
                    workflowContext.PushScheduledActivity(destinationActivity);
                }
            }
        }
    }
}
