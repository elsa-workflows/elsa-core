using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Persistence.Models;
using Flowsharp.Persistence.Specifications;

namespace Flowsharp.Persistence.InMemory
{
    public class InMemoryWorkflowInstanceStore : IWorkflowInstanceStore
    {
        public InMemoryWorkflowInstanceStore()
        {
            WorkflowInstances = new List<WorkflowInstance>();
        }

        public IList<WorkflowInstance> WorkflowInstances { get; }
        
        public Task<IEnumerable<WorkflowInstance>> GetManyAsync(ISpecification<WorkflowInstance, IWorkflowInstanceSpecificationVisitor> specification, CancellationToken cancellationToken)
        {
            var query = WorkflowInstances.AsQueryable().Where(x => specification.IsSatisfiedBy(x));
            var matches = query.Distinct().ToList();
            return Task.FromResult<IEnumerable<WorkflowInstance>>(matches);
        }

        public Task AddAsync(WorkflowInstance value, CancellationToken cancellationToken)
        {
            WorkflowInstances.Add(value);
            return Task.CompletedTask;
        }
        
        public Task UpdateAsync(WorkflowInstance value, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}