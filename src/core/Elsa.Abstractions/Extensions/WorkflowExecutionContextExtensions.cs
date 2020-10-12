using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
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

        public static IEnumerable<ConnectionDefinition>
            GetInboundConnections(this WorkflowExecutionContext workflowExecutionContext, string activityId) =>
            workflowExecutionContext.WorkflowDefinition.Connections.Where(x => x.TargetActivityId == activityId);

        public static IEnumerable<ConnectionDefinition> GetOutboundConnections(
            this WorkflowExecutionContext workflowExecutionContext,
            string activityId) =>
            workflowExecutionContext.WorkflowDefinition.Connections.Where(x => x.SourceActivityId == activityId);

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
                if (connection.SourceActivityId == startingPointActivityId)
                    yield break;

                yield return connection.SourceActivityId!;

                foreach (var parentActivityId in workflowExecutionContext
                    .GetInboundActivityPathInternal(connection.SourceActivityId!, startingPointActivityId)
                    .Distinct())
                    yield return parentActivityId;
            }
        }
    }
}