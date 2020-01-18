using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Messages.Domain.Handlers
{
    public class PersistenceWorkflowEventHandler :
        INotificationHandler<WorkflowExecuted>,
        INotificationHandler<WorkflowSuspended>,
        INotificationHandler<ActivityExecuted>,
        INotificationHandler<WorkflowCompleted>
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly ILogger logger;

        public PersistenceWorkflowEventHandler(IWorkflowInstanceStore workflowInstanceStore, ILogger<PersistenceWorkflowEventHandler> logger)
        {
            this.workflowInstanceStore = workflowInstanceStore;
            this.logger = logger;
        }

        public async Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.Suspended)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.WorkflowExecuted)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.ActivityExecuted)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;

            if (workflowExecutionContext.DeleteCompletedInstances || workflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.Suspended)
            {
                logger.LogDebug("Deleting completed workflow instance {WorkflowInstanceId}", workflowExecutionContext.InstanceId);
                await workflowInstanceStore.DeleteAsync(workflowExecutionContext.InstanceId, cancellationToken);
            }
        }

        private async Task SaveWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var workflowInstance = await workflowInstanceStore.GetByIdAsync(workflowExecutionContext.InstanceId, cancellationToken);

            if (workflowInstance == null)
                workflowInstance = workflowExecutionContext.CreateWorkflowInstance();
            else
                workflowInstance = workflowExecutionContext.UpdateWorkflowInstance(workflowInstance);

            await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        }
    }
}