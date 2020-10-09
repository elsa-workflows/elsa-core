using System.Collections.Generic;
using System.Linq;
using Elsa.Indexes;
using Elsa.Models;
using YesSql;

namespace Elsa.Extensions
{
    public static class WorkflowDefinitionExtensions
    {
        public static IEnumerable<WorkflowDefinition> WithVersion(
            this IEnumerable<WorkflowDefinition> query,
            VersionOptions version)
        {
            return query.AsQueryable().WithVersion(version);
        }
        
        public static IQueryable<WorkflowDefinition> WithVersion(
            this IQueryable<WorkflowDefinition> query,
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
        
        public static IQuery<WorkflowDefinition> WithVersion(
            this IQuery<WorkflowDefinition> query,
            VersionOptions version)
        {
            var index = query.With<WorkflowDefinitionIndex>();
            
            if (version.IsDraft)
                query = index.Where(x => !x.IsPublished);
            else if (version.IsLatest)
                query = index.Where(x => x.IsLatest);
            else if (version.IsPublished)
                query = index.Where(x => x.IsPublished);
            else if (version.IsLatestOrPublished)
                query = index.Where(x => x.IsPublished || x.IsLatest);
            else if (version.AllVersions)
            {
                // Nothing to filter.
            }
            else if (version.Version > 0)
                query = index.Where(x => x.Version == version.Version);

            return index.OrderByDescending(x => x.Version);
        }
    }
}