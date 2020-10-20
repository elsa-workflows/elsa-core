using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Data;
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

        public ValueTask SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            _session.Save(workflowDefinition, CollectionNames.WorkflowDefinitions);
            return new ValueTask();
        }

        public ValueTask DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            _session.Delete(workflowDefinition, CollectionNames.WorkflowDefinitions);
            return new ValueTask();
        }

        public async ValueTask<IEnumerable<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken = default) => await Query().ListAsync();
        public IQuery<WorkflowDefinition> Query() => _session.Query<WorkflowDefinition>(CollectionNames.WorkflowDefinitions);
        public IQuery<WorkflowDefinition, TIndex> Query<TIndex>() where TIndex : class, IIndex => _session.Query<WorkflowDefinition, TIndex>(CollectionNames.WorkflowDefinitions);
        public IQuery<WorkflowDefinition, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex => _session.Query<WorkflowDefinition, TIndex>(predicate, CollectionNames.WorkflowDefinitions);
        public IQuery<WorkflowDefinition> ExecuteQuery(ICompiledQuery<WorkflowDefinition> query) => _session.ExecuteQuery(query, CollectionNames.WorkflowDefinitions);
    }
}