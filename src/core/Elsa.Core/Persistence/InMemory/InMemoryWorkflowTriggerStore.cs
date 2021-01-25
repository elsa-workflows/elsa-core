using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowTriggerStore : InMemoryStore<WorkflowTrigger>, IWorkflowTriggerStore
    {
        public InMemoryWorkflowTriggerStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}