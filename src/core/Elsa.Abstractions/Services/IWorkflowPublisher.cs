using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IWorkflowPublisher
    {
        Task<WorkflowDefinition> PublishAsync(string id, CancellationToken cancellationToken);
        Task<WorkflowDefinition> PublishAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken);
        Task<WorkflowDefinition> GetDraftAsync(string id, CancellationToken cancellationToken);
    }
}