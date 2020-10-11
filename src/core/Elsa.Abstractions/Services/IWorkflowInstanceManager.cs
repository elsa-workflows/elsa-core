using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Services
{
    public interface IWorkflowInstanceManager
    {
        ValueTask SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        ValueTask DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        IQuery<WorkflowInstance> Query();
        IQuery<WorkflowInstance, TIndex> Query<TIndex>() where TIndex : class, IIndex;
        IQuery<WorkflowInstance, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex;
        IQuery<WorkflowInstance> ExecuteQuery(ICompiledQuery<WorkflowInstance> query);
    }
}