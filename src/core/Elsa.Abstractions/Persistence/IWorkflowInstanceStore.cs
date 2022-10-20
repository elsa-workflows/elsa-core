using Elsa.Models;
using Elsa.Persistence.Specifications;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Persistence
{
    public interface IWorkflowInstanceStore : IStore<WorkflowInstance>
    {
        //This method allow the use to get only specific field from the store repository, which enable to perform better queries.
        Task<IEnumerable<TOut>> FindManyAsync<TOut>(ISpecification<WorkflowInstance> specification, Expression<Func<WorkflowInstance, TOut>> funcMapping, IOrderBy<WorkflowInstance>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default) where TOut : class;

    }
}