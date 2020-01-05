using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
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
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.Suspended)
                await SaveWorkflowAsync(cancellationToken);
        }
        
        public async Task Handle(WorkflowExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.WorkflowExecuted)
                await SaveWorkflowAsync(cancellationToken);
        }
        
        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.ActivityExecuted)
                await SaveWorkflowAsync(cancellationToken);
        }
        
        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            var workflow = notification.WorkflowExecutionContext;
            var blueprint = workflow;
            
            if (blueprint.DeleteCompletedInstances || blueprint.PersistenceBehavior == WorkflowPersistenceBehavior.Suspended)
                await workflowInstanceStore.DeleteAsync(workflow.InstanceId, cancellationToken);
        }

        private async Task SaveWorkflowAsync(CancellationToken cancellationToken)
        {
            //var workflowInstance = workflow.ToInstance();
            //await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            throw new NotImplementedException();
        }
    }
}