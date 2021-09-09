using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;

namespace Elsa.Services.Workflows
{
    public class WorkflowInstanceDeleter : IWorkflowInstanceDeleter
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public WorkflowInstanceDeleter(IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowInstanceStore = workflowInstanceStore;
        }

        public async Task<DeleteWorkflowInstanceResult> DeleteAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(workflowInstanceId, cancellationToken);

            if (workflowInstance == null)
                return new DeleteWorkflowInstanceResult(DeleteWorkflowInstanceResultStatus.NotFound, null);

            await _workflowInstanceStore.DeleteAsync(workflowInstance, cancellationToken);

            return new DeleteWorkflowInstanceResult(DeleteWorkflowInstanceResultStatus.Ok, workflowInstance);
        }
    }
}