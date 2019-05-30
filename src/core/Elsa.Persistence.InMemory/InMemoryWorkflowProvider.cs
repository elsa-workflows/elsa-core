using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowProvider : IInMemoryWorkflowProvider
    {
        private readonly HashSet<Workflow> workflows = new HashSet<Workflow>();
        
        public Task SaveAsync(Workflow value, CancellationToken cancellationToken)
        {
            workflows.Add(value);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Workflow>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Workflow>>(workflows.ToList()); 
        }
    }
}