using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using YesSql;

namespace Elsa.Persistence.YesSql.Extensions
{
    public static class WorkflowDefinitionExtensions
    {
        public static IQuery<WorkflowDefinitionDocument, WorkflowDefinitionIndex> WithVersion(this IQuery<WorkflowDefinitionDocument, WorkflowDefinitionIndex> query, VersionOptions version)
        {
            if (version.IsDraft)
                query = query.Where(x => !x.IsPublished).OrderByDescending(x => x.Version);
            else if(version.IsLatest)
                query = query.Where(x => x.IsLatest);
            else if(version.IsPublished)
                query = query.Where(x => x.IsPublished).OrderByDescending(x => x.Version);
            else if(version.Version > 0)
                query = query.Where(x => x.Version == version.Version);

            return query;
        }
    }
}