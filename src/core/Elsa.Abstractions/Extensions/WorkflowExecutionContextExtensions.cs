using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowExecutionContextExtensions
    {
        // public static IEnumerable<IActivity> GetStartActivities(this WorkflowExecutionContext workflowExecutionContext)
        // {
        //     var targetActivities = workflowExecutionContext.Connections.Select(x => x.Target.Activity).Distinct().ToLookup(x => x);
        //
        //     var query =
        //         from activity in workflowExecutionContext.Activities
        //         where !targetActivities.Contains(activity)
        //         select activity;
        //
        //     return query;
        // }

        public static IEnumerable<IConnection>
            GetInboundConnections(this WorkflowExecutionContext workflowExecutionContext, string activityId) =>
            workflowExecutionContext.WorkflowBlueprint.Connections.Where(x => x.Target.Activity.Id == activityId);

        public static IEnumerable<IConnection> GetOutboundConnections(
            this WorkflowExecutionContext workflowExecutionContext,
            string activityId) =>
            workflowExecutionContext.WorkflowBlueprint.Connections.Where(x => x.Source.Activity.Id == activityId);

        public static IEnumerable<string> GetInboundActivityPath(this WorkflowExecutionContext workflowExecutionContext,
            string activityId) =>
            workflowExecutionContext.GetInboundActivityPathInternal(activityId, activityId).Distinct();

        private static IEnumerable<string> GetInboundActivityPathInternal(
            this WorkflowExecutionContext workflowExecutionContext,
            string activityId,
            string startingPointActivityId)
        {
            foreach (var connection in workflowExecutionContext.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.Source.Activity.Id == startingPointActivityId)
                    yield break;

                yield return connection.Source.Activity.Id!;

                foreach (var parentActivityId in workflowExecutionContext
                    .GetInboundActivityPathInternal(connection.Source.Activity.Id!, startingPointActivityId)
                    .Distinct())
                    yield return parentActivityId;
            }
        }
    }
}