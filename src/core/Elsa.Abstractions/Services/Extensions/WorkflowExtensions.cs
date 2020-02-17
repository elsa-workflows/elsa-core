using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Services.Extensions
{
    public static class WorkflowExtensions
    {
        public static bool IsExecuting(this Workflow workflow) => workflow.Status == WorkflowStatus.Executing;

        public static bool IsFaultedOrAborted(this Workflow workflow) =>
            workflow.Status == WorkflowStatus.Aborted ||
            workflow.Status == WorkflowStatus.Faulted;

        public static bool IsFinished(this Workflow workflow) => workflow.Status == WorkflowStatus.Finished;

        public static IEnumerable<IActivity> GetStartActivities(this Workflow workflow)
        {
            var targetActivityIds = workflow.Connections.Select(x => x.Target.Activity.Id).Distinct().ToLookup(x => x);

            var query =
                from activity in workflow.Activities
                where !targetActivityIds.Contains(activity.Id)
                select activity;

            return query;
        }

        public static IActivity GetActivity(this Workflow workflow, string id) =>
            workflow.Activities.FirstOrDefault(x => x.Id == id);

        public static LogEntry AddLogEntry(
            this Workflow workflow, 
            IActivity activity, 
            Instant instant, 
            string message,
            bool faulted = false)
        {
            var entry = new LogEntry(activity.Id, instant, message, faulted);
            workflow.ExecutionLog.Add(entry);
            return entry;
        }

        public static IEnumerable<Connection> GetInboundConnections(this Workflow workflow, string activityId)
        {
            return workflow.Connections.Where(x => x.Target.Activity.Id == activityId).ToList();
        }

        public static IEnumerable<Connection> GetOutboundConnections(this Workflow workflow, string activityId)
        {
            return workflow.Connections.Where(x => x.Source.Activity.Id == activityId).ToList();
        }

        /// <summary>
        /// Returns the full path of incoming activities.
        /// </summary>
        public static IEnumerable<string> GetInboundActivityPath(this Workflow workflow, string activityId)
        {
            var inspectedActivityIDs = new HashSet<string>();

            return workflow.GetInboundActivityPathInternal(activityId, activityId, inspectedActivityIDs)
                           .Distinct().ToList();
        }

        private static IEnumerable<string> GetInboundActivityPathInternal(this Workflow workflowInstance,
            string activityId, 
            string startingPointActivityId,
            HashSet<string> inspectedActivityIDs)
        {
            foreach (var connection in workflowInstance.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (inspectedActivityIDs.Contains(connection.Source.Activity.Id))
                    yield break;

                yield return connection.Source.Activity.Id;

                foreach (var parentActivityId in workflowInstance.GetInboundActivityPathInternal(connection.Source.Activity.Id, startingPointActivityId, inspectedActivityIDs)
                                                                 .Distinct())
                {
                    inspectedActivityIDs.Add(parentActivityId);
                    yield return parentActivityId;
                }
            }
        }
    }
}