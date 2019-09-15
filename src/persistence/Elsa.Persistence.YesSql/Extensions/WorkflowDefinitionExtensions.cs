using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using YesSql;

namespace Elsa.Persistence.YesSql.Extensions
{
    public static class WorkflowDefinitionExtensions
    {
        public static IQuery<WorkflowDefinitionVersionDocument, WorkflowDefinitionIndex> WithVersion(
            this IQuery<WorkflowDefinitionVersionDocument, WorkflowDefinitionIndex> query,
            VersionOptions version)
        {
            return query.WithVersion(new[] { version });
        }
        

        public static IQuery<WorkflowDefinitionVersionDocument, WorkflowDefinitionIndex> WithVersion(
            this IQuery<WorkflowDefinitionVersionDocument, WorkflowDefinitionIndex> query, 
            IEnumerable<VersionOptions> versions)
        {
            // TODO: Reimplement this to be the same as the generic IQueryable extension.
            var version = versions.First();
            
            if (version.IsDraft)
                query = query.Where(x => !x.IsPublished).OrderByDescending(x => x.Version);
            if (version.IsLatest)
                query = query.Where(x => x.IsLatest);
            if (version.IsPublished)
                query = query.Where(x => x.IsPublished).OrderByDescending(x => x.Version);
            if (version.Version > 0)
                query = query.Where(x => x.Version == version.Version);

            return query;
        }
    }
}