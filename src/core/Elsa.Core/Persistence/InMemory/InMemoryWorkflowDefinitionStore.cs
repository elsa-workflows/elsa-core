using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowDefinitionStore : InMemoryStore<WorkflowDefinition>, IWorkflowDefinitionStore
    {
        public InMemoryWorkflowDefinitionStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}