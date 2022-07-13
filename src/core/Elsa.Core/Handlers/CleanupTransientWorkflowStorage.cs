using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Providers.WorkflowStorage;
using MediatR;

namespace Elsa.Handlers
{
    public class CleanupTransientWorkflowStorage : INotificationHandler<WorkflowExecutionFinished>
    {
        private readonly TransientWorkflowStorageProvider _transientWorkflowStorageProvider;
        public CleanupTransientWorkflowStorage(TransientWorkflowStorageProvider transientWorkflowStorageProvider) => _transientWorkflowStorageProvider = transientWorkflowStorageProvider;
        
        public async Task Handle(WorkflowExecutionFinished notification, CancellationToken cancellationToken)
        {
            var workflowStorageContext = new WorkflowStorageContext(notification.WorkflowExecutionContext.WorkflowInstance, null);
            await _transientWorkflowStorageProvider.DeleteAsync(workflowStorageContext, cancellationToken);
        }
    }
}