using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives.Activities;
using Elsa.Extensions;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Activities.Primitives.Drivers
{
    public class JoinDriver : ActivityDriver<Join>, IWorkflowEventHandler
    {
        protected override ActivityExecutionResult OnExecute(Join activity, WorkflowExecutionContext workflowContext)
        {
            var branches = activity.InboundTransitions ?? new List<string>();
            var workflow = workflowContext.Workflow;
            var inboundTransitions = workflow.GetInboundConnections(activity.Id);
            var done = false;

            switch (activity.Mode)
            {
                case Join.JoinMode.WaitAll:
                    done = inboundTransitions.All(x => branches.Contains(GetTransitionKey(x)));
                    break;
                case Join.JoinMode.WaitAny:
                    done = inboundTransitions.Any(x => branches.Contains(GetTransitionKey(x)));

                    if (done)
                    {
                        // Remove any inbound blocking activities.
                        var ancestorActivityIds = workflow.GetInboundActivityPath(activity.Id).ToList();
                        var blockingActivities = workflow.BlockingActivities.Where(x => ancestorActivityIds.Contains(x.Id)).ToList();

                        foreach (var blockingActivity in blockingActivities)
                        {
                            workflow.BlockingActivities.Remove(blockingActivity);
                        }
                    }

                    break;
            }

            if (done)
            {
                return Endpoint("Done");
            }

            return Noop();
        }

        private void RecordInboundTransitions(WorkflowExecutionContext workflowContext, IActivity activity)
        {
            var workflow = workflowContext.Workflow;

            // Get outbound connections of the executing activity.
            var outboundConnections = workflow.GetOutboundConnections(activity.Id);

            // Get any connection that is pointing to this activity.
            var inboundTransitionsQuery =
                from connection in outboundConnections
                let destinationActivity = connection.Target.Activity
                where destinationActivity.Name == nameof(Join)
                select connection;

            var inboundConnections = inboundTransitionsQuery.ToList();

            // For each inbound connection, record the transition.
            foreach (var inboundConnection in inboundConnections)
            {
                var joinActivity = (Join) inboundConnection.Target.Activity;
                var inboundTransitions = joinActivity.InboundTransitions ?? new List<string>();
                joinActivity.InboundTransitions = inboundTransitions.Union(new[] { GetTransitionKey(inboundConnection) }).Distinct().ToList();
            }
        }

        private string GetTransitionKey(Connection connection)
        {
            var sourceActivityId = connection.Source.Activity.Id;
            var sourceOutcomeName = connection.Source.Name;

            return $"@{sourceActivityId}_{sourceOutcomeName}";
        }

        Task IWorkflowEventHandler.ActivityExecutedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken)
        {
            RecordInboundTransitions(workflowContext, activity);

            return Task.CompletedTask;
        }
    }
}