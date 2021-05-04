using System.Threading;
using System.Threading.Tasks;

using Elsa.Events;
using Elsa.Indexing.Services;

using MediatR;

namespace Elsa.Indexing.Handlers
{
    public class UpdateWorkflowDefinitionIndexHandler : INotificationHandler<WorkflowDefinitionSaved>, INotificationHandler<WorkflowDefinitionDeleted>
    {
        private readonly IWorkflowDefinitionIndexer _instanceIndexer;

        public UpdateWorkflowDefinitionIndexHandler(IWorkflowDefinitionIndexer instanceIndexer)
        {
            _instanceIndexer = instanceIndexer;
        }

        public Task Handle(WorkflowDefinitionSaved notification, CancellationToken cancellationToken)
        {
            return _instanceIndexer.IndexAsync(notification.WorkflowDefinition, cancellationToken);
        }

        public Task Handle(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
        {
            return _instanceIndexer.DeleteAsync(notification.WorkflowDefinition, cancellationToken);
        }
    }
}
