using System.Collections.Generic;
using System.Linq;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowExecutionContextExtensions
    {
        public static IEnumerable<IActivity> GetStartActivities(this WorkflowExecutionContext workflowExecutionContext)
        {
            var targetActivities = workflowExecutionContext.Connections.Select(x => x.Target.Activity).Distinct().ToLookup(x => x);

            var query =
                from activity in workflowExecutionContext.Activities
                where !targetActivities.Contains(activity)
                select activity;

            return query;
        }
        
        public static IEnumerable<Connection> GetInboundConnections(this WorkflowExecutionContext workflowExecutionContext, IActivity activity) => workflowExecutionContext.Connections.Where(x => x.Target.Activity == activity);
        public static IEnumerable<Connection> GetOutboundConnections(this WorkflowExecutionContext workflowExecutionContext, IActivity activity) => workflowExecutionContext.Connections.Where(x => x.Source.Activity == activity);
        public static IEnumerable<string> GetInboundActivityPath(this WorkflowExecutionContext workflowExecutionContext, IActivity activity) => workflowExecutionContext.GetInboundActivityPathInternal(activity, activity).Distinct();

        private static IEnumerable<string> GetInboundActivityPathInternal(this WorkflowExecutionContext workflowExecutionContext, IActivity activity, IActivity startingPointActivity)
        {
            foreach (var connection in workflowExecutionContext.GetInboundConnections(activity))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.Source.Activity == startingPointActivity)
                    yield break;

                yield return connection.Source.Activity.Id;

                foreach (var parentActivityId in workflowExecutionContext
                    .GetInboundActivityPathInternal(connection.Source.Activity, startingPointActivity)
                    .Distinct())
                    yield return parentActivityId;
            }
        }
    }
}