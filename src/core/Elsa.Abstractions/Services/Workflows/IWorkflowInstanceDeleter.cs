using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IWorkflowInstanceDeleter
    {
        Task<DeleteWorkflowInstanceResult> DeleteAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    }

    public record DeleteWorkflowInstanceResult(DeleteWorkflowInstanceResultStatus Status, WorkflowInstance? WorkflowInstance);

    public enum DeleteWorkflowInstanceResultStatus
    {
        Ok,
        NotFound
    }
}