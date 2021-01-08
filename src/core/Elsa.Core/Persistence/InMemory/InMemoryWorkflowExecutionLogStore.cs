using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowExecutionLogStore : InMemoryStore<WorkflowExecutionLogRecord>, IWorkflowExecutionLogStore
    {
        public InMemoryWorkflowExecutionLogStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}