using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowInstanceStore : InMemoryStore<WorkflowInstance>, IWorkflowInstanceStore
    {
        public InMemoryWorkflowInstanceStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}