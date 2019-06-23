using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Services.Extensions
{
    public static class WorkflowBlueprintExtensions
    {
        public static IEnumerable<ActivityBlueprint> GetStartActivities(this WorkflowBlueprint workflow)
        {
            var targetActivityIds = workflow.Connections.Select(x => x.Target.ActivityId).Distinct().ToLookup(x => x);
            
            var query =
                from activity in workflow.Activities
                where !targetActivityIds.Contains(activity.Id)
                select activity;

            return query;
        }

        public static ActivityBlueprint GetActivity(this WorkflowBlueprint workflowBlueprint, string id) => workflowBlueprint.Activities.FirstOrDefault(x => x.Id == id);

        public static IEnumerable<ConnectionBlueprint> GetInboundConnections(this WorkflowBlueprint workflowBlueprint, string activityId)
        {
            return workflowBlueprint.Connections.Where(x => x.Target.ActivityId == activityId).ToList();
        }

        public static IEnumerable<ConnectionBlueprint> GetOutboundConnections(this WorkflowBlueprint workflowBlueprint, string activityId)
        {
            return workflowBlueprint.Connections.Where(x => x.Source.ActivityId == activityId).ToList();
        }

        /// <summary>
        /// Returns the full path of incoming activities.
        /// </summary>
        public static IEnumerable<string> GetInboundActivityPath(this WorkflowBlueprint workflowBlueprint, string activityId)
        {
            return workflowBlueprint.GetInboundActivityPathInternal(activityId, activityId).Distinct().ToList();
        }

        private static IEnumerable<string> GetInboundActivityPathInternal(this WorkflowBlueprint workflowBlueprint, string activityId, string startingPointActivityId)
        {
            foreach (var connection in workflowBlueprint.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.Source.ActivityId == startingPointActivityId)
                    yield break;

                yield return connection.Source.ActivityId;

                foreach (var parentActivityId in workflowBlueprint.GetInboundActivityPathInternal(connection.Source.ActivityId, startingPointActivityId).Distinct())
                    yield return parentActivityId;
            }
        }
    }
}