using System.Linq;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Entities;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions
{
    public static class WorkflowDefinitionDocumentExtensions
    {
        public static IQueryable<WorkflowDefinitionVersionEntity> WithVersion(
            this IQueryable<WorkflowDefinitionVersionEntity> query,
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