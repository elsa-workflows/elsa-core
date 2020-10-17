using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
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
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly ILogger _logger;

        public PersistenceWorkflowEventHandler(
            IWorkflowInstanceManager workflowInstanceManager,
            ILogger<PersistenceWorkflowEventHandler> logger)
        {
            _workflowInstanceManager = workflowInstanceManager;
            _logger = logger;
        }

        public async Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior == WorkflowPersistenceBehavior.Suspended)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior ==
                WorkflowPersistenceBehavior.WorkflowPassCompleted)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior ==
                WorkflowPersistenceBehavior.ActivityExecuted || notification.Activity.PersistWorkflow)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;

            if (workflowExecutionContext.DeleteCompletedInstances)
            {
                _logger.LogDebug(
                    "Deleting completed workflow instance {WorkflowInstanceId}",
                    workflowExecutionContext.WorkflowInstance.WorkflowInstanceId);
                await _workflowInstanceManager.DeleteAsync(
                    workflowExecutionContext.WorkflowInstance,
                    cancellationToken);
            }
            else
            {
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
            }
        }

        private async ValueTask SaveWorkflowAsync(
            WorkflowExecutionContext workflowExecutionContext,
            CancellationToken cancellationToken)
        {
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            await _workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);
        }
    }
}