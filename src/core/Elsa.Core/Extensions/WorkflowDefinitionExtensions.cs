using System.Linq;
using Elsa.Models;

namespace Elsa.Extensions
{
    public static class WorkflowDefinitionExtensions
    {
        public static IQueryable<WorkflowDefinition> WithVersion(this IQueryable<WorkflowDefinition> query, VersionOptions version)
        {
            if (version.IsDraft)
                query = query.Where(x => !x.IsPublished).OrderByDescending(x => x.Version);
            else if(version.IsLatest)
                query = query.OrderByDescending(x => x.Version);
            else if(version.IsPublished)
                query = query.Where(x => x.IsPublished).OrderByDescending(x => x.Version);
            else if(version.Version > 0)
                query = query.Where(x => x.Version == version.Version);

            return query;
        }
    }
}