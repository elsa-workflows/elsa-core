using Elsa.Models;
using Elsa.Services;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowExecutionLogStore : InMemoryStore<WorkflowExecutionLogRecord>, IWorkflowExecutionLogStore
    {
        public InMemoryWorkflowExecutionLogStore(IIdGenerator idGenerator) : base(idGenerator)
        {
        }
    }
}