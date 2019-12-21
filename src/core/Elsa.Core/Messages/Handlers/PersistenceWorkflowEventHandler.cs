using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Messages.Handlers
{
    public class PersistenceWorkflowEventHandler : 
        INotificationHandler<WorkflowExecuted>, 
        INotificationHandler<WorkflowSuspended>,
        INotificationHandler<ActivityExecuted>,
        INotificationHandler<WorkflowCompleted>
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public PersistenceWorkflowEventHandler(IWorkflowInstanceStore workflowInstanceStore)
        {
            this.workflowInstanceStore = workflowInstanceStore;
        }
        
        public async Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
        {
            if (notification.Workflow.Definition.PersistenceBehavior == WorkflowPersistenceBehavior.Suspended)
                await SaveWorkflowAsync(notification.Workflow, cancellationToken);
        }
        
        public async Task Handle(WorkflowExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.Workflow.Definition.PersistenceBehavior == WorkflowPersistenceBehavior.WorkflowExecuted)
                await SaveWorkflowAsync(notification.Workflow, cancellationToken);
        }
        
        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.Workflow.Definition.PersistenceBehavior == WorkflowPersistenceBehavior.ActivityExecuted)
                await SaveWorkflowAsync(notification.Workflow, cancellationToken);
        }
        
        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            if (notification.Workflow.Definition.DeleteCompletedWorkflows)
                await workflowInstanceStore.DeleteAsync(notification.Workflow.Id, cancellationToken);
        }

        private async Task SaveWorkflowAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var workflowInstance = workflow.ToInstance();
            await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        }
    }
}