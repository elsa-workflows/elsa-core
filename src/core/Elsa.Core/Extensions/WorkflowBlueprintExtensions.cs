using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class WorkflowBlueprintExtensions
    {
        public static IEnumerable<WorkflowBlueprint> WithVersion(
            this IEnumerable<WorkflowBlueprint> query,
            VersionOptions version)
        {
            return query.AsQueryable().WithVersion(version);
        }
        
        public static IQueryable<WorkflowBlueprint> WithVersion(
            this IQueryable<WorkflowBlueprint> query,
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
    }
}