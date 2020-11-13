using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Data;
using Elsa.Extensions;
using Elsa.Models;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Services
{
    public class WorkflowDefinitionManager : IWorkflowDefinitionManager
    {
        private readonly ISession _session;

        public WorkflowDefinitionManager(ISession session)
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
            await Query()
                .WithVersion(version ?? VersionOptions.Latest)
                .CountAsync();
        
        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(int? skip = default, int? take = default, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            var query = Query().WithVersion(version ?? VersionOptions.Latest);

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
    }
}