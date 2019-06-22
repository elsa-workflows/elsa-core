using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Serialization.Models;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowProvider : IInMemoryWorkflowProvider
    {
        private readonly HashSet<WorkflowInstance> workflows = new HashSet<WorkflowInstance>();
        
        public Task SaveAsync(WorkflowInstance value, CancellationToken cancellationToken)
        {
            workflows.Add(value);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<WorkflowInstance>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<WorkflowInstance>>(workflows.ToList()); 
        }
    }
}