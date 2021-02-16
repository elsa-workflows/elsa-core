using System.Collections.Generic;
using System.Linq;
using ElsaDashboard.Application.Models;

namespace ElsaDashboard.Application.Extensions
{
    public static class WorkflowModelExtensions
    {
        public static IEnumerable<ActivityModel> GetChildActivities(this WorkflowModel workflowModel, string? parentId)
        {
            if (parentId == null)
            {
                var targetIds = workflowModel.Connections.Select(x => x.TargetId).Distinct().ToLookup(x => x);
                var children = workflowModel.Activities.Where(x => !targetIds.Contains(x.ActivityId)).ToList();

                return children;
            }
            else
            {
                var targetIds = workflowModel.Connections.Where(x => x.SourceId == parentId).Select(x => x.TargetId).Distinct().ToLookup(x => x);
                var children = workflowModel.Activities.Where(x => targetIds.Contains(x.ActivityId)).ToList();
                
                return children;
            }
        }

        public static IEnumerable<ActivityModel> GetLeafActivities(this WorkflowModel workflowModel)
        {
            var sourceIds = workflowModel.Connections.Select(x => x.SourceId).Distinct().ToLookup(x => x);
            var leaves = workflowModel.Activities.Where(x => !sourceIds.Contains(x.ActivityId)).ToList();

            return leaves;
        }

        public static IEnumerable<ConnectionModel> GetInboundConnections(this WorkflowModel workflowModel, string activityId) => workflowModel.Connections.Where(x => x.TargetId == activityId).ToList();

        public static IEnumerable<ConnectionModel> GetOutboundConnections(this WorkflowModel workflowModel, string activityId) => workflowModel.Connections.Where(x => x.SourceId == activityId).ToList();

        /// <summary>
        /// Returns the full path of incoming activities.
        /// </summary>
        public static IEnumerable<string> GetInboundActivityPath(this WorkflowModel workflowModel, string activityId)
        {
            var inspectedActivityIds = new HashSet<string>();

            return workflowModel.GetInboundActivityPathInternal(activityId, inspectedActivityIds)
                .Distinct().ToList();
        }

        private static IEnumerable<string> GetInboundActivityPathInternal(this WorkflowModel workflowModel, string activityId, HashSet<string> inspectedActivityIds)
        {
            foreach (var connection in workflowModel.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (inspectedActivityIds.Contains(connection.SourceId))
                    yield break;

                yield return connection.SourceId;

                foreach (var parentActivityId in workflowModel.GetInboundActivityPathInternal(connection.SourceId, inspectedActivityIds).Distinct())
                {
                    inspectedActivityIds.Add(parentActivityId);
                    yield return parentActivityId;
                }
            }
        }
    }
}