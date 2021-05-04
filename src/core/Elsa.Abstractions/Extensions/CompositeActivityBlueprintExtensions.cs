using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa
{
    public static class CompositeActivityBlueprintExtensions
    {
        public static IEnumerable<IActivityBlueprint> GetEndActivities(this ICompositeActivityBlueprint workflowBlueprint) => workflowBlueprint.Activities.Where(x => !workflowBlueprint.GetOutboundConnections(x.Id).Any());
        public static IActivityBlueprint? GetActivity(this ICompositeActivityBlueprint workflowBlueprint, string id) => workflowBlueprint.Activities.FirstOrDefault(x => x.Id == id);
        public static IEnumerable<IActivityBlueprint> GetActivities(this ICompositeActivityBlueprint workflowBlueprint, IEnumerable<string> ids) => workflowBlueprint.Activities.Where(x => ids.Contains(x.Id));

        public static IEnumerable<IActivityBlueprint> GetBlockingActivities(this ICompositeActivityBlueprint workflowBlueprint, WorkflowInstance workflowInstance) =>
            workflowBlueprint.Activities.Where(activity => workflowInstance.BlockingActivities.Select(x => x.ActivityId).Contains(activity.Id));

        public static IEnumerable<IConnection> GetInboundConnections(this ICompositeActivityBlueprint workflowBlueprint, string activityId) =>
            workflowBlueprint.Connections.Where(x => x.Target.Activity.Id == activityId).ToList();

        public static IEnumerable<IConnection> GetOutboundConnections(this ICompositeActivityBlueprint workflowBlueprint, string activityId) =>
            workflowBlueprint.Connections.Where(x => x.Source.Activity.Id == activityId).ToList();
        
        public static IEnumerable<IConnection> GetInboundConnectionPath(this ICompositeActivityBlueprint workflowBlueprint, string activityId)
        {
            var inspectedActivityIds = new HashSet<string>();
            return workflowBlueprint.GetInboundConnectionPathInternal(activityId, inspectedActivityIds);
        }
        
        private static IEnumerable<IConnection> GetInboundConnectionPathInternal(this ICompositeActivityBlueprint workflowBlueprintBlueprintInstance, string activityId, HashSet<string> inspectedActivityIds)
        {
            foreach (var connection in workflowBlueprintBlueprintInstance.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (inspectedActivityIds.Contains(connection.Source.Activity.Id))
                    yield break;

                yield return connection;

                foreach (var childConnection in workflowBlueprintBlueprintInstance.GetInboundConnectionPathInternal(connection.Source.Activity.Id, inspectedActivityIds).Distinct())
                {
                    inspectedActivityIds.Add(childConnection.Source.Activity.Id);
                    yield return childConnection;
                }
            }
        }

        /// <summary>
        /// Returns the full path of incoming activities.
        /// </summary>
        public static IEnumerable<string> GetInboundActivityPath(this ICompositeActivityBlueprint workflowBlueprint, string activityId)
        {
            var inspectedActivityIds = new HashSet<string>();
            return workflowBlueprint.GetInboundActivityPathInternal(activityId, inspectedActivityIds).Distinct().ToList();
        }

        private static IEnumerable<string> GetInboundActivityPathInternal(this ICompositeActivityBlueprint workflowBlueprintBlueprintInstance, string activityId, HashSet<string> inspectedActivityIds)
        {
            foreach (var connection in workflowBlueprintBlueprintInstance.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (inspectedActivityIds.Contains(connection.Source.Activity.Id))
                    yield break;

                yield return connection.Source.Activity.Id;

                foreach (var parentActivityId in workflowBlueprintBlueprintInstance.GetInboundActivityPathInternal(connection.Source.Activity.Id, inspectedActivityIds).Distinct())
                {
                    inspectedActivityIds.Add(parentActivityId);
                    yield return parentActivityId;
                }
            }
        }
    }
}