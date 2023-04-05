using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Contracts
{
    public interface IWorkflowDefinitionImporter
    {
        Task<WorkflowDefinition> ImportAsync(SaveWorkflowDefinitionRequest model, CancellationToken cancellationToken = default);
    }
}
