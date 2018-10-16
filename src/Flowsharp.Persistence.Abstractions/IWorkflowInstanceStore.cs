using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Persistence.Models;
using Flowsharp.Persistence.Specifications;

namespace Flowsharp.Persistence
{
    public interface IWorkflowInstanceStore
    {
        Task<IEnumerable<WorkflowInstance>> GetManyAsync(ISpecification<WorkflowInstance, IWorkflowInstanceSpecificationVisitor> specification, CancellationToken cancellationToken);
        Task AddAsync(WorkflowInstance value, CancellationToken cancellationToken);
        Task UpdateAsync(WorkflowInstance value, CancellationToken cancellationToken);
    }
}