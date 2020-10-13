using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowExtensions
    {
        public static IEnumerable<IWorkflowBlueprint> WithVersion(
            this IEnumerable<IWorkflowBlueprint> query,
            VersionOptions version)
            => query.AsQueryable().WithVersion(version);

        public static IQueryable<IWorkflowBlueprint> WithVersion(
            this IQueryable<IWorkflowBlueprint> query,
            VersionOptions version)
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

        public static IEnumerable<IActivityBlueprint> GetStartActivities(this IWorkflowBlueprint workflowBlueprint)
        {
            var targetActivityIds = workflowBlueprint.Connections.Select(x => x.Target.Activity.Id).Distinct()
                .ToLookup(x => x);

            var query =
                from activity in workflowBlueprint.Activities
                where !targetActivityIds.Contains(activity.Id)
                select activity;

            return query;
        }

        public static IActivityBlueprint? GetActivity(this IWorkflowBlueprint workflowBlueprint, string id) =>
            workflowBlueprint.Activities.FirstOrDefault(x => x.Id == id);

        public static IEnumerable<IConnection> GetInboundConnections(
            this IWorkflowBlueprint workflowBlueprint,
            string activityId) => workflowBlueprint.Connections.Where(x => x.Target.Activity.Id == activityId).ToList();

        public static IEnumerable<IConnection> GetOutboundConnections(
            this IWorkflowBlueprint workflowBlueprint,
            string activityId) =>
            workflowBlueprint.Connections.Where(x => x.Source.Activity.Id == activityId).ToList();

        /// <summary>
        /// Returns the full path of incoming activities.
        /// </summary>
        public static IEnumerable<string> GetInboundActivityPath(
            this IWorkflowBlueprint workflowBlueprint,
            string activityId)
        {
            var inspectedActivityIds = new HashSet<string>();

            return workflowBlueprint.GetInboundActivityPathInternal(activityId, inspectedActivityIds)
                .Distinct().ToList();
        }

        private static IEnumerable<string> GetInboundActivityPathInternal(
            this IWorkflowBlueprint workflowBlueprintBlueprintInstance,
            string activityId,
            HashSet<string> inspectedActivityIds)
        {
            foreach (var connection in workflowBlueprintBlueprintInstance.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (inspectedActivityIds.Contains(connection.Source.Activity.Id))
                    yield break;

                yield return connection.Source.Activity.Id;

                foreach (var parentActivityId in workflowBlueprintBlueprintInstance.GetInboundActivityPathInternal(
                        connection.Source.Activity.Id,
                        inspectedActivityIds)
                    .Distinct())
                {
                    inspectedActivityIds.Add(parentActivityId);
                    yield return parentActivityId;
                }
            }
        }
    }
}