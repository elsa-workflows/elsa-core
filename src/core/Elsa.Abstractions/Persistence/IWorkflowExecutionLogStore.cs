using Elsa.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowExecutionLogStore : IStore<WorkflowExecutionLogRecord>
    {
    }
}