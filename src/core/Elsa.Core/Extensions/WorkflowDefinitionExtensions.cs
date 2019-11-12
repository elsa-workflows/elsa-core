using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Extensions
{
    public static class WorkflowDefinitionExtensions
    {
        public static IEnumerable<WorkflowDefinitionVersion> WithVersion(
            this IEnumerable<WorkflowDefinitionVersion> query,
            VersionOptions version)
        {
            return query.AsQueryable().WithVersion(version);
        }
        
        public static IQueryable<WorkflowDefinitionVersion> WithVersion(
            this IQueryable<WorkflowDefinitionVersion> query,
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