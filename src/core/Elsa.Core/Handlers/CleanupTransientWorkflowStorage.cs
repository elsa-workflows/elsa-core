using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Providers.WorkflowStorage;
using MediatR;

namespace Elsa.Handlers
{
    public class CleanupTransientWorkflowStorage : INotificationHandler<WorkflowExecutionBurstCompleted>
    {
        private readonly TransientWorkflowStorageProvider _transientWorkflowStorageProvider;
        public CleanupTransientWorkflowStorage(TransientWorkflowStorageProvider transientWorkflowStorageProvider) => _transientWorkflowStorageProvider = transientWorkflowStorageProvider;
        
        public async Task Handle(WorkflowExecutionBurstCompleted notification, CancellationToken cancellationToken)
        {
            var workflowStorageContext = new WorkflowStorageContext(notification.ActivityExecutionContext.WorkflowInstance, notification.ActivityExecutionContext.ActivityId);
            await _transientWorkflowStorageProvider.DeleteAsync(workflowStorageContext, cancellationToken);
        }
    }
}