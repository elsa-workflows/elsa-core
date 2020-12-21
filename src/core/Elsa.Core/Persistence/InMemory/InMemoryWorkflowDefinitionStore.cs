using Elsa.Models;
using Elsa.Services;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowDefinitionStore : InMemoryStore<WorkflowDefinition>, IWorkflowDefinitionStore
    {
        public InMemoryWorkflowDefinitionStore(IIdGenerator idGenerator) : base(idGenerator)
        {
        }
    }
}