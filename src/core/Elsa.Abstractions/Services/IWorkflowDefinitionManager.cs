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
        Task SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        Task<int> CountAsync(VersionOptions? version = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowDefinition>> ListAsync(int? skip = default, int? take = default, VersionOptions? version = default, CancellationToken cancellationToken = default);
        IQuery<WorkflowDefinition> Query();
        IQuery<WorkflowDefinition, TIndex> Query<TIndex>() where TIndex : class, IIndex;

        IQuery<WorkflowDefinition, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate)
            where TIndex : class, IIndex;

        IQuery<WorkflowDefinition> ExecuteQuery(ICompiledQuery<WorkflowDefinition> query);
        WorkflowDefinition Initialize(WorkflowDefinition workflowDefinition);
    }
}