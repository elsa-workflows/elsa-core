using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
using Elsa.Triggers;
using MediatR;

namespace Elsa.Handlers
{
    public class UpdateTriggers : INotificationHandler<WorkflowInstanceSaved>, INotificationHandler<ManyWorkflowInstancesDeleted>, INotificationHandler<ManyWorkflowInstancesAdded>
    {
        private readonly IWorkflowTriggerIndexer _workflowTriggerIndexer;

        public UpdateTriggers(IWorkflowTriggerIndexer workflowTriggerIndexer)
        {
            _workflowTriggerIndexer = workflowTriggerIndexer;
        }

        public async Task Handle(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowInstance;
            await _workflowTriggerIndexer.IndexTriggersAsync(workflowInstance, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesDeleted notification, CancellationToken cancellationToken)
        {
            var workflowInstanceIds = notification.WorkflowInstances.Select(x => x.Id).ToList();
            await _workflowTriggerIndexer.DeleteTriggersAsync(workflowInstanceIds, cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesAdded notification, CancellationToken cancellationToken)
        {
            await _workflowTriggerIndexer.IndexTriggersAsync(notification.WorkflowInstances, cancellationToken);
        }
    }
}