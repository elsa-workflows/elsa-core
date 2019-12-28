using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class WorkflowBlueprintExtensions
    {
        public static IEnumerable<IActivity> GetStartActivities(this WorkflowBlueprint workflow)
        {
            var targetActivityIds =
                workflow.Connections.Select(x => x.Target.Activity.Id).Distinct().ToLookup(x => x);

            var query =
                from activity in workflow.Activities
                where !targetActivityIds.Contains(activity.Id)
                select activity;

            var activities = query.ToArray();
            return activities.Any() ? activities : workflow.Activities.Take(1);
        }

        public static IActivity GetActivity(this WorkflowBlueprint workflowDefinition, string id) => workflowDefinition.Activities.FirstOrDefault(x => x.Id == id);
        public static IEnumerable<Connection> GetInboundConnections(this WorkflowBlueprint workflowDefinition, string activityId) => workflowDefinition.Connections.Where(x => x.Target.Activity.Id == activityId).ToList();
        public static IEnumerable<Connection> GetOutboundConnections(this WorkflowBlueprint workflowDefinition, string activityId) => workflowDefinition.Connections.Where(x => x.Source.Activity.Id == activityId).ToList();

        /// <summary>
        /// Returns the full path of incoming activities.
        /// </summary>
        public static IEnumerable<string> GetInboundActivityPath(this WorkflowBlueprint workflowDefinition, string activityId)
        {
            return workflowDefinition.GetInboundActivityPathInternal(activityId, activityId).Distinct().ToList();
        }

        private static IEnumerable<string> GetInboundActivityPathInternal(this WorkflowBlueprint workflowDefinition, string activityId, string startingPointActivityId)
        {
            foreach (var connection in workflowDefinition.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.Source.Activity.Id == startingPointActivityId)
                    yield break;

                yield return connection.Source.Activity.Id;

                foreach (var parentActivityId in workflowDefinition
                    .GetInboundActivityPathInternal(connection.Source.Activity.Id, startingPointActivityId)
                    .Distinct())
                    yield return parentActivityId;
            }
        }
    }
}