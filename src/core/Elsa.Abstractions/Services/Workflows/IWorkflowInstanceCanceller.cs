using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IWorkflowInstanceCanceller
    {
        Task<CancelWorkflowInstanceResult> CancelAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
    }

    public record CancelWorkflowInstanceResult(CancelWorkflowInstanceResultStatus Status, WorkflowInstance? WorkflowInstance);

    public enum CancelWorkflowInstanceResultStatus
    {
        Ok,
        NotFound,
        InvalidStatus
    }
}