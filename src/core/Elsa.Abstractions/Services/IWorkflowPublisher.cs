using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IWorkflowPublisher
    {
        WorkflowDefinitionVersion New();

        Task<WorkflowDefinitionVersion> PublishAsync(string id, CancellationToken cancellationToken = default);

        Task<WorkflowDefinitionVersion> PublishAsync(
            WorkflowDefinitionVersion workflowDefinitionVersion,
            CancellationToken cancellationToken = default);

        Task<WorkflowDefinitionVersion> GetDraftAsync(string id, CancellationToken cancellationToken = default);

        Task<WorkflowDefinitionVersion> SaveDraftAsync(
            WorkflowDefinitionVersion workflowDefinitionVersion,
            CancellationToken cancellationToken = default);
    }
}