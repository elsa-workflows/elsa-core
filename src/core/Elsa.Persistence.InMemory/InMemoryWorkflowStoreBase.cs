using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence.InMemory
{
    public abstract class InMemoryWorkflowStoreBase : IWorkflowStore
    {
        protected IInMemoryWorkflowProvider Provider { get; }

        protected InMemoryWorkflowStoreBase(IInMemoryWorkflowProvider provider)
        {
            this.Provider = provider;
        }

        public Task<IEnumerable<Workflow>> ListAllAsync(CancellationToken cancellationToken)
        {
            return ListAllAsync(null, cancellationToken);
        }
        
        public async Task<IEnumerable<Workflow>> ListAllAsync(string parentId, CancellationToken cancellationToken)
        {
            var workflows = await Provider.ListAsync(cancellationToken);

            if (parentId != null)
                workflows = workflows.Where(x => x.ParentId == parentId);

            return workflows;
        }

        public async Task<Workflow> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(cancellationToken);

            return workflows.FirstOrDefault(x => x.Id == id);
        }

        public async Task SaveAsync(Workflow value, CancellationToken cancellationToken)
        {
            await Provider.SaveAsync(value, cancellationToken);
        }
    }
}