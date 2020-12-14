using Elsa.Models;
using Elsa.Services;

namespace Elsa.Persistence.InMemory
{
    public class InMemorySuspendedWorkflowStore : InMemoryStore<SuspendedWorkflowBlockingActivity>, ISuspendedWorkflowStore
    {
        public InMemorySuspendedWorkflowStore(IIdGenerator idGenerator) : base(idGenerator)
        {
        }
    }
}