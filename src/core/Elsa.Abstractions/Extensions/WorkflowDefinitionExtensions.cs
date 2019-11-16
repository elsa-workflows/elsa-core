using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Extensions
{
    public static class WorkflowDefinitionExtensions
    {
        public static IEnumerable<ActivityDefinition> GetStartActivities(this WorkflowDefinitionVersion workflow)
        {
            var destinationActivityIds =
                workflow.Connections.Select(x => x.DestinationActivityId).Distinct().ToLookup(x => x);

            var query =
                from activity in workflow.Activities
                where !destinationActivityIds.Contains(activity.Id)
                select activity;

            var activities = query.ToArray();
            return activities.Any() ? activities : workflow.Activities.Take(1);
        }

        public static ActivityDefinition GetActivity(this WorkflowDefinitionVersion workflowDefinition, string id) =>
            workflowDefinition.Activities.FirstOrDefault(x => x.Id == id);

        public static IEnumerable<ConnectionDefinition> GetInboundConnections(
            this WorkflowDefinitionVersion workflowDefinition,
            string activityId)
        {
            return workflowDefinition.Connections.Where(x => x.DestinationActivityId == activityId).ToList();
        }

        public static IEnumerable<ConnectionDefinition> GetOutboundConnections(
            this WorkflowDefinitionVersion workflowDefinition, string activityId)
        {
            return workflowDefinition.Connections.Where(x => x.SourceActivityId == activityId).ToList();
        }

        /// <summary>
        /// Returns the full path of incoming activities.
        /// </summary>
        public static IEnumerable<string> GetInboundActivityPath(this WorkflowDefinitionVersion workflowDefinition,
            string activityId)
        {
            return workflowDefinition.GetInboundActivityPathInternal(activityId, activityId).Distinct().ToList();
        }

        private static IEnumerable<string> GetInboundActivityPathInternal(
            this WorkflowDefinitionVersion workflowDefinition, string activityId, string startingPointActivityId)
        {
            foreach (var connection in workflowDefinition.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.SourceActivityId == startingPointActivityId)
                    yield break;

                yield return connection.SourceActivityId;

                foreach (var parentActivityId in workflowDefinition
                    .GetInboundActivityPathInternal(connection.SourceActivityId, startingPointActivityId)
                    .Distinct())
                    yield return parentActivityId;
            }
        }
    }
}