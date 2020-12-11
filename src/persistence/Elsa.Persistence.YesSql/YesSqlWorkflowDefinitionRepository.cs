using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Data;
using Elsa.Models;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Repositories;

using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql
{
    public class YesSqlWorkflowDefinitionRepository : IWorkflowDefinitionRepository
    {
        private readonly ISession _session;
        private readonly Elsa.Services.IIdGenerator _idGenerator;

        public YesSqlWorkflowDefinitionRepository(ISession session, Elsa.Services.IIdGenerator idGenerator)
        {
            _session = session;
            _idGenerator = idGenerator;
        }

        public Task SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            workflowDefinition = Initialize(workflowDefinition);
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
        public IQuery<WorkflowDefinition, TIndex> Query<TIndex>() where TIndex : class, IIndex => _session.Query<WorkflowDefinition, TIndex>(CollectionNames.WorkflowDefinitions);
        public IQuery<WorkflowDefinition, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex => _session.Query<WorkflowDefinition, TIndex>(predicate, CollectionNames.WorkflowDefinitions);
        public IQuery<WorkflowDefinition> ExecuteQuery(ICompiledQuery<WorkflowDefinition> query) => _session.ExecuteQuery(query, CollectionNames.WorkflowDefinitions);

        public WorkflowDefinition Initialize(WorkflowDefinition workflowDefinition)
        {
            if (string.IsNullOrWhiteSpace(workflowDefinition.WorkflowDefinitionId))
                workflowDefinition.WorkflowDefinitionId = _idGenerator.Generate();

            if (workflowDefinition.Version == 0)
                workflowDefinition.Version = 1;

            if (string.IsNullOrWhiteSpace(workflowDefinition.WorkflowDefinitionVersionId))
                workflowDefinition.WorkflowDefinitionVersionId = _idGenerator.Generate();

            return workflowDefinition;
        }

        public Task<WorkflowDefinition> GetAsync(string workflowDefinitionId, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = Query<WorkflowDefinitionIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId);

            return WithVersion(query, version).FirstOrDefaultAsync();
        }

        public Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
        {
            return Query<WorkflowDefinitionIndex>(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId).FirstOrDefaultAsync();
        }

        private IQuery<WorkflowDefinition> WithVersion(IQuery<WorkflowDefinition> query,
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
