using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Data;
using Elsa.Models;
using Elsa.Persistence.YesSql.Indexes;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql
{
    public class YesSqlWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly ISession _session;

        public YesSqlWorkflowDefinitionStore(ISession session)
        {
            _session = session;
        }

        public Task SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            _session.Save(workflowDefinition, CollectionNames.WorkflowDefinitions);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            _session.Delete(workflowDefinition, CollectionNames.WorkflowDefinitions);
            return Task.CompletedTask;
        }

        public async Task<int> CountAsync(VersionOptions? version = default, CancellationToken cancellationToken = default) =>
            await WithVersion(Query(), version ?? VersionOptions.Latest)
                .CountAsync();

        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(int? skip = default, int? take = default, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            var query = WithVersion(Query(), version ?? VersionOptions.Latest);

            if (skip != null)
                query = query.Skip(skip.Value);

            if (take != null)
                query = query.Take(take.Value);

            return await query.ListAsync();
        }

        public IQuery<WorkflowDefinition> Query() => _session.Query<WorkflowDefinition>(CollectionNames.WorkflowDefinitions);
        public IQuery<WorkflowDefinition, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex => _session.Query<WorkflowDefinition, TIndex>(predicate, CollectionNames.WorkflowDefinitions);

        public Task<WorkflowDefinition> GetAsync(string workflowDefinitionId, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = Query<WorkflowDefinitionIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId);
            return WithVersion(query, version).FirstOrDefaultAsync();
        }

        public Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) => Query<WorkflowDefinitionIndex>(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId).FirstOrDefaultAsync();

        private static IQuery<WorkflowDefinition> WithVersion(
            IQuery<WorkflowDefinition> query,
            VersionOptions version)
        {
            var index = query.With<WorkflowDefinitionIndex>();

            if (version.IsDraft)
                index = index.Where(x => !x.IsPublished);
            else if (version.IsLatest)
                index = index.Where(x => x.IsLatest);
            else if (version.IsPublished)
                index = index.Where(x => x.IsPublished);
            else if (version.IsLatestOrPublished)
                index = index.Where(x => x.IsPublished || x.IsLatest);
            else if (version.AllVersions)
            {
                // Nothing to filter.
            }
            else if (version.Version > 0)
                index = index.Where(x => x.Version == version.Version);

            return index.OrderByDescending(x => x.Version);
        }
    }
}