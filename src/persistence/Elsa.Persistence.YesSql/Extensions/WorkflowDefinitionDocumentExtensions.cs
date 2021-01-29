using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using YesSql;

namespace Elsa.Persistence.YesSql
{
    public static class WorkflowDefinitionDocumentExtensions
    {
        public static bool WithVersion(this WorkflowDefinitionDocument workflowDefinition, VersionOptions? version = default)
        {
            var versionOption = version ?? VersionOptions.Latest;

            if (versionOption.IsDraft)
                return !workflowDefinition.IsPublished;
            if (versionOption.IsLatest)
                return workflowDefinition.IsLatest;
            if (versionOption.IsPublished)
                return workflowDefinition.IsPublished;
            if (versionOption.IsLatestOrPublished)
                return workflowDefinition.IsPublished || workflowDefinition.IsLatest;
            if (versionOption.Version > 0)
                return workflowDefinition.Version == versionOption.Version;

            return true;
        }
        
        public static IQuery<WorkflowDefinitionDocument, WorkflowDefinitionIndex> WithVersion(this IQuery<WorkflowDefinitionDocument, WorkflowDefinitionIndex> query, VersionOptions? version = default)
        {
            var versionOption = version ?? VersionOptions.Latest;

            if (versionOption.IsDraft)
                return query.Where(x => !x.IsPublished);
            if (versionOption.IsLatest)
                return query.Where(x => x.IsLatest);
            if (versionOption.IsPublished)
                return query.Where(x => x.IsPublished);
            if (versionOption.IsLatestOrPublished)
                return query.Where(x => x.IsPublished || x.IsLatest);
            if (versionOption.Version > 0)
                return query.Where(x => x.Version == versionOption.Version);

            return query;
        }
    }
}