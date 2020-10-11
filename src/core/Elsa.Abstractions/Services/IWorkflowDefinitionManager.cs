using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Services
{
    public interface IWorkflowDefinitionManager
    {
        ValueTask SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        ValueTask DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        ValueTask<IEnumerable<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken = default);
        IQuery<WorkflowDefinition> Query();
        IQuery<WorkflowDefinition, TIndex> Query<TIndex>() where TIndex : class, IIndex;

        IQuery<WorkflowDefinition, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate)
            where TIndex : class, IIndex;

        IQuery<WorkflowDefinition> ExecuteQuery(ICompiledQuery<WorkflowDefinition> query);
    }
}