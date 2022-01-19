using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface ITriggerIndexer
    {
        Task IndexTriggersAsync(CancellationToken cancellationToken = default);
        Task IndexTriggersAsync(IEnumerable<WorkflowDefinition> workflowDefinitions, CancellationToken cancellationToken = default);
        Task IndexTriggersAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        Task DeleteTriggersAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default);
        Task DeleteTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    }
}