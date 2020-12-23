using Elsa.Models;
using Elsa.Services;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowInstanceStore : InMemoryStore<WorkflowInstance>, IWorkflowInstanceStore
    {
        public InMemoryWorkflowInstanceStore(IIdGenerator idGenerator) : base(idGenerator)
        {
        }
    }
}