using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IWorkflowPublisher
    {
        WorkflowDefinitionVersion New();
        
        Task<WorkflowDefinitionVersion> PublishAsync(string id, CancellationToken cancellationToken);

        Task<WorkflowDefinitionVersion> PublishAsync(
            WorkflowDefinitionVersion workflowDefinition,
            CancellationToken cancellationToken);

        Task<WorkflowDefinitionVersion> GetDraftAsync(string id, CancellationToken cancellationToken);

        Task<WorkflowDefinitionVersion> SaveDraftAsync(
            WorkflowDefinitionVersion workflowDefinition,
            CancellationToken cancellationToken);
    }
}