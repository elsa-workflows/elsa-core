using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using MediatR;
using ProcessInstance = Elsa.Services.Models.ProcessInstance;

namespace Elsa.Messages.Handlers
{
    public class PersistenceWorkflowEventHandler : 
        INotificationHandler<ProcessExecuted>, 
        INotificationHandler<ProcessSuspended>,
        INotificationHandler<ActivityExecuted>,
        INotificationHandler<ProcessCompleted>
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public PersistenceWorkflowEventHandler(IWorkflowInstanceStore workflowInstanceStore)
        {
            this.workflowInstanceStore = workflowInstanceStore;
        }
        
        public async Task Handle(ProcessSuspended notification, CancellationToken cancellationToken)
        {
            if (notification.Process.Blueprint.PersistenceBehavior == ProcessPersistenceBehavior.Suspended)
                await SaveWorkflowAsync(notification.Process, cancellationToken);
        }
        
        public async Task Handle(ProcessExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.Process.Blueprint.PersistenceBehavior == ProcessPersistenceBehavior.WorkflowExecuted)
                await SaveWorkflowAsync(notification.Process, cancellationToken);
        }
        
        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.Process.Blueprint.PersistenceBehavior == ProcessPersistenceBehavior.ActivityExecuted)
                await SaveWorkflowAsync(notification.Process, cancellationToken);
        }
        
        public async Task Handle(ProcessCompleted notification, CancellationToken cancellationToken)
        {
            var workflow = notification.Process;
            var blueprint = workflow.Blueprint;
            
            if (blueprint.DeleteCompletedInstances || blueprint.PersistenceBehavior == ProcessPersistenceBehavior.Suspended)
                await workflowInstanceStore.DeleteAsync(workflow.Id, cancellationToken);
        }

        private async Task SaveWorkflowAsync(ProcessInstance workflow, CancellationToken cancellationToken)
        {
            //var workflowInstance = workflow.ToInstance();
            //await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            throw new NotImplementedException();
        }
    }
}