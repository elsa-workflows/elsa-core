using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface ITriggerIndexer
    {
        Task IndexTriggersAsync(CancellationToken cancellationToken = default);
        Task IndexTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints, Tenant tenant, CancellationToken cancellationToken = default);
        Task IndexTriggersAsync(IWorkflowBlueprint workflowBlueprint, Tenant tenant, CancellationToken cancellationToken = default);
        Task DeleteTriggersAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    }
}