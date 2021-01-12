using System.Linq;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public abstract class IteratingActivity : Activity, IBranchingActivity
    {
        public void Unwind(ActivityExecutionContext context)
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
            var currentActivityId = context.ActivityBlueprint.Id;
            var inboundConnections = workflowBlueprint.GetInboundConnectionPath(currentActivityId).ToList();

            var query = 
                from inboundConnection in inboundConnections 
                let parentActivityBlueprint = inboundConnection.Source.Activity 
                where inboundConnection.Source.Activity.Type == Type
                select inboundConnection;

            var firstMatch = query.FirstOrDefault();
            
            if(firstMatch != null && firstMatch.Source.Outcome == OutcomeNames.Iterate)
                workflowExecutionContext.ScheduleActivity(firstMatch.Source.Activity.Id);
        }
    }
}