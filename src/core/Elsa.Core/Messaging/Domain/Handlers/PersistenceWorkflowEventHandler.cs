using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Messaging.Domain.Handlers
{
    public class PersistenceWorkflowEventHandler :
        INotificationHandler<WorkflowExecuted>,
        INotificationHandler<WorkflowSuspended>,
        INotificationHandler<ActivityExecuted>,
        INotificationHandler<WorkflowCompleted>
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly ILogger _logger;

        public PersistenceWorkflowEventHandler(IWorkflowInstanceStore workflowInstanceStore, ILogger<PersistenceWorkflowEventHandler> logger)
        {
            this._workflowInstanceStore = workflowInstanceStore;
            this._logger = logger;
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
            if (notification.WorkflowExecutionContext.PersistenceBehavior == WorkflowPersistenceBehavior.ActivityExecuted || notification.Activity.PersistWorkflow)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;

            if (workflowExecutionContext.DeleteCompletedInstances)
            {
                _logger.LogDebug("Deleting completed workflow instance {WorkflowInstanceId}", workflowExecutionContext.InstanceId);
                await _workflowInstanceStore.DeleteAsync(workflowExecutionContext.InstanceId, cancellationToken);
            }
            else
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        private async Task SaveWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var workflowInstance = await _workflowInstanceStore.GetByIdAsync(workflowExecutionContext.InstanceId, cancellationToken);

            if (workflowInstance == null)
                workflowInstance = workflowExecutionContext.CreateWorkflowInstance();
            else
                workflowInstance = workflowExecutionContext.UpdateWorkflowInstance(workflowInstance);

            await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        }
    }
}