using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using System.Linq;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    public static class WorkflowDefinitionDocumentExtensions
    {
        public static IQueryable<WorkflowDefinitionVersionDocument> WithVersion(
            this IQueryable<WorkflowDefinitionVersionDocument> query,
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