using System.Linq;
using Elsa.Models;

namespace Elsa.Extensions
{
    public static class WorkflowDefinitionExtensions
    {
        public static IQueryable<WorkflowDefinition> WithVersion(
            this IQueryable<WorkflowDefinition> query,
            VersionOptions version)
        {

            if (version.IsDraft)
                query = query.Where(x => !x.IsPublished);
            if (version.IsLatest)
                query = query.OrderByDescending(x => x.Version);
            if (version.IsPublished)
                query = query.Where(x => x.IsPublished);
            if (version.IsLatestOrPublished)
                query = query.Where(x => x.IsPublished || x.IsLatest);
            if (version.Version > 0)
                query = query.Where(x => x.Version == version.Version);

            return query.OrderByDescending(x => x.Version);
        }
    }
}