using System.Collections.Generic;
using System.Linq;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        DisplayName = "If/Else",
        Category = "Control Flow",
        Description = "Evaluate a Boolean expression and continue execution depending on the result.",
        Outcomes = new[] { True, False, OutcomeNames.Done }
    )]
    public class IfElse : Activity, IBranchingActivity
    {
        public const string True = "True";
        public const string False = "False";

        [ActivityProperty(Hint = "The condition to evaluate.")]
        public bool Condition { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var outcome = Condition ? True : False;
            return Outcome(outcome);
        }

        public void Unwind(ActivityExecutionContext context)
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
            var activityExecutionContext = context;
            var currentActivityBlueprint = activityExecutionContext.ActivityBlueprint;
            var currentActivityId = currentActivityBlueprint.Id;

            // An IfElse activity completed within a burst of execution, which means its outcome (true or false) did not yield child activities to execute (an empty branch).
            // Schedule its child activities connected to the "Done" outcome.
            if (currentActivityBlueprint.Type == nameof(IfElse))
            {
                var nextActivities = GetNextActivities(workflowExecutionContext, currentActivityId);
                workflowExecutionContext.ScheduleActivities(nextActivities);
            }
            else
            {
                // Get all incoming connections.
                var inboundConnections = workflowBlueprint.GetInboundConnectionPath(currentActivityId).ToList();

                // Filter out those connections who have a source of IfElse.
                var query =
                    from inboundConnection in inboundConnections
                    let parentActivityBlueprint = inboundConnection.Source.Activity
                    where inboundConnection.Source.Activity.Type == nameof(IfElse)
                    select inboundConnection;

                var firstMatch = query.FirstOrDefault();
                
                if (firstMatch != null && firstMatch.Source.Outcome != OutcomeNames.Done)
                {
                    var parentActivityBlueprint = firstMatch.Source.Activity;
                    var nextActivities = OutcomeResult.GetNextActivities(workflowExecutionContext, parentActivityBlueprint.Id, new[] { OutcomeNames.Done }).ToList();
                    workflowExecutionContext.ScheduleActivities(nextActivities);
                }
            }
        }

        private IEnumerable<string> GetNextActivities(WorkflowExecutionContext workflowExecutionContext, string currentActivityId) => OutcomeResult.GetNextActivities(workflowExecutionContext, currentActivityId, new[] { OutcomeNames.Done });
    }
}