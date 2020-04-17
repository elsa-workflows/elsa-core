using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class WorkflowExtensions
    {
        public static IEnumerable<Workflow> WithVersion(this IEnumerable<Workflow> query, VersionOptions version) 
            => query.AsQueryable().WithVersion(version);

        public static IQueryable<Workflow> WithVersion(this IQueryable<Workflow> query, VersionOptions version)
        {
            if (version.IsDraft)
                query = query.Where(x => !x.IsPublished);
            else if (version.IsLatest)
                query = query.Where(x => x.IsLatest);
            else if (version.IsPublished)
                query = query.Where(x => x.IsPublished);
            else if (version.IsLatestOrPublished)
                query = query.Where(x => x.IsPublished || x.IsLatest);
            else if (version.AllVersions)
            {
                // Nothing to filter.
            }
            else if (version.Version > 0)
                query = query.Where(x => x.Version == version.Version);

            return query.OrderByDescending(x => x.Version);
        }
        
        public static IEnumerable<IActivity> GetStartActivities(this Workflow workflow)
        {
            var targetActivityIds = workflow.Connections.Select(x => x.Target.Activity.Id).Distinct().ToLookup(x => x);

            var query =
                from activity in workflow.Activities
                where !targetActivityIds.Contains(activity.Id)
                select activity;

            return query;
        }

        public static IActivity GetActivity(this Workflow workflow, string id) => workflow.Activities.FirstOrDefault(x => x.Id == id);

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