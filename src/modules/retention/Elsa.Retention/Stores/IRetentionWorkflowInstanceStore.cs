using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using System.Linq.Expressions;
using System;

namespace Elsa.Retention.Stores
{
    public interface IRetentionWorkflowInstanceStore : IWorkflowInstanceStore
    {
        Task<IEnumerable<Tdto>> FindManyWithDTOAsync<Tdto>(ISpecification<WorkflowInstance> specification, IOrderBy<WorkflowInstance>? orderBy = default, IPaging? paging = default, Expression<Func<WorkflowInstance, Tdto>> selectDtoFunc = default, CancellationToken cancellationToken = default) where Tdto : class;

    }

}