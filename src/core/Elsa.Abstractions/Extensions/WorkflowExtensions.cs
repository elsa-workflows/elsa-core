using System.Collections.Generic;
using System.Linq;
using Elsa.Comparers;
using Elsa.Models;
using NodaTime;

namespace Elsa.Extensions
{
    public static class WorkflowExtensions
    {
        public static bool IsDefinition(this Workflow workflow) => string.IsNullOrWhiteSpace(workflow.ParentId);
        public static bool IsInstance(this Workflow workflow) => !workflow.IsDefinition();

        public static bool IsHalted(this Workflow workflow) =>
            workflow.Status == WorkflowStatus.Halted;

        public static bool IsFaulted(this Workflow workflow) =>
            workflow.Status == WorkflowStatus.Aborted ||
            workflow.Status == WorkflowStatus.Faulted;

        public static bool IsFinished(this Workflow workflow) =>
            workflow.Status == WorkflowStatus.Finished;

        public static IEnumerable<IActivity> GetStartActivities(this Workflow workflow)
        {
            var targetActivities = workflow.Connections.Select(x => x.Target.Activity).Distinct(new ActivityEqualityComparer()).ToDictionary(x => x.Id);
            
            var query =
                from activity in workflow.Activities
                where !targetActivities.ContainsKey(activity.Id)
                select activity;

            return query;
        }

        public static IActivity GetActivity(this Workflow workflow, string id) => workflow.Activities.SingleOrDefault(x => x.Id == id);
        public static T GetActivity<T>(this Workflow workflow, string id) where T : IActivity => (T) workflow.GetActivity(id);

        public static LogEntry AddLogEntry(this Workflow workflow, string activityId, Instant instant, string message, bool faulted = false)
        {
            var entry = new LogEntry(activityId, instant, message, faulted);
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
            return workflow.GetInboundActivityPathInternal(activityId, activityId).Distinct().ToList();
        }

        private static IEnumerable<string> GetInboundActivityPathInternal(this Workflow workflow, string activityId, string startingPointActivityId)
        {
            foreach (var connection in workflow.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.Source.Activity.Id == startingPointActivityId)
                    yield break;

                yield return connection.Source.Activity.Id;

                foreach (var parentActivityId in workflow.GetInboundActivityPathInternal(connection.Source.Activity.Id, startingPointActivityId).Distinct())
                    yield return parentActivityId;
            }
        }
    }
}