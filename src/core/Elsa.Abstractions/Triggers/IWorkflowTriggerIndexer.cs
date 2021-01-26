using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Triggers
{
    public interface IWorkflowTriggerIndexer
    {
        Task IndexTriggersAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task DeleteTriggersAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default);
        Task DeleteTriggersAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    }
}