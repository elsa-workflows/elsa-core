using System.Collections.Generic;
using System.Linq;

using Elsa.Models;

namespace Elsa.Repositories
{
    public static class IWorkflowDefinitionRepositoryExtensions
    {
        public static IEnumerable<WorkflowDefinition> WithVersion(this IEnumerable<WorkflowDefinition> query, VersionOptions? version = default)
        {
            var versionOption = version ?? VersionOptions.Latest;

            if (versionOption.IsDraft)
                return query.Where(x => !x.IsPublished);
            else if (versionOption.IsLatest)
                return query.Where(x => x.IsLatest);
            else if (versionOption.IsPublished)
                return query.Where(x => x.IsPublished);
            else if (versionOption.IsLatestOrPublished)
                return query.Where(x => x.IsPublished || x.IsLatest);
            else if (versionOption.Version > 0)
                return query.Where(x => x.Version == versionOption.Version);

            return query;
        }

        public static IQueryable<WorkflowDefinition> WithVersion(this IQueryable<WorkflowDefinition> query, VersionOptions? version = default)
        {
            var versionOption = version ?? VersionOptions.Latest;

            if (versionOption.IsDraft)
                return query.Where(x => !x.IsPublished);
            else if (versionOption.IsLatest)
                return query.Where(x => x.IsLatest);
            else if (versionOption.IsPublished)
                return query.Where(x => x.IsPublished);
            else if (versionOption.IsLatestOrPublished)
                return query.Where(x => x.IsPublished || x.IsLatest);
            else if (versionOption.Version > 0)
                return query.Where(x => x.Version == versionOption.Version);

            return query;
        }
    }
}
