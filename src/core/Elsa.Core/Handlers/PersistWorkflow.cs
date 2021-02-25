using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Handlers
{
    public class PersistWorkflow :
        INotificationHandler<WorkflowExecuted>,
        INotificationHandler<WorkflowSuspended>,
        INotificationHandler<WorkflowExecutionPassCompleted>,
        INotificationHandler<WorkflowExecutionBurstStarting>,
        INotificationHandler<WorkflowExecutionBurstCompleted>,
        INotificationHandler<WorkflowExecutionFinished>
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly ILogger _logger;

        public PersistWorkflow(IWorkflowInstanceStore workflowInstanceStore, ILogger<PersistWorkflow> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _logger = logger;
        }

        public async Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior == WorkflowPersistenceBehavior.Suspended)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowExecutionBurstStarting notification, CancellationToken cancellationToken)
        {
            var behavior = notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior;
            
            if (behavior == WorkflowPersistenceBehavior.WorkflowBurst || behavior == WorkflowPersistenceBehavior.WorkflowPassCompleted)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowExecutionBurstCompleted notification, CancellationToken cancellationToken)
        {
            var behavior = notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior;
            
            if (behavior == WorkflowPersistenceBehavior.WorkflowBurst)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowExecutionPassCompleted notification, CancellationToken cancellationToken)
        {
            var behavior = notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior;

            if (behavior == WorkflowPersistenceBehavior.WorkflowPassCompleted || notification.ActivityExecutionContext.ActivityBlueprint.PersistWorkflow)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowExecuted notification, CancellationToken cancellationToken)
        {
            var behavior = notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior;

            if (behavior == WorkflowPersistenceBehavior.WorkflowPassCompleted || behavior == WorkflowPersistenceBehavior.WorkflowBurst)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
        }

        public async Task Handle(WorkflowExecutionFinished notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;

            if (workflowExecutionContext.DeleteCompletedInstances)
            {
                _logger.LogDebug("Deleting completed workflow instance {WorkflowInstanceId}", workflowExecutionContext.WorkflowInstance.Id);
                await _workflowInstanceStore.DeleteAsync(workflowExecutionContext.WorkflowInstance, cancellationToken);
            }
            else
            {
                var behavior = notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior;

                if (behavior == WorkflowPersistenceBehavior.WorkflowPassCompleted || behavior == WorkflowPersistenceBehavior.WorkflowBurst)
                    await SaveWorkflowAsync(notification.WorkflowExecutionContext, cancellationToken);
            }
        }

        private async ValueTask SaveWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            workflowExecutionContext.PruneActivityData();
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            _logger.LogDebug("Committed workflow {WorkflowInstanceId} to storage", workflowInstance.Id);
        }
    }
}