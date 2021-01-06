using System.Threading;
using System.Threading.Tasks;

using Elsa.Events;
using Elsa.Indexing.Services;

using MediatR;

namespace Elsa.Indexing.Handlers
{
    public class WorkflowInstanceUpdateHandler : INotificationHandler<WorkflowInstanceSaved>, INotificationHandler<WorkflowInstanceDeleted>
    {
        private readonly IWorkflowInstanceIndexer _instanceIndexer;

        public WorkflowInstanceUpdateHandler(IWorkflowInstanceIndexer instanceIndexer)
        {
            _instanceIndexer = instanceIndexer;
        }

        public Task Handle(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
        {
            return _instanceIndexer.IndexAsync(notification.WorkflowInstance, cancellationToken);
        }

        public Task Handle(WorkflowInstanceDeleted notification, CancellationToken cancellationToken)
        {
            return _instanceIndexer.DeleteAsync(notification.WorkflowInstance, cancellationToken);
        }
    }
}
