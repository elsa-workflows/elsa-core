using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Handlers
{
    public class PersistWorkflow :
        INotificationHandler<WorkflowFaulted>,
        INotificationHandler<WorkflowExecutionPassCompleted>,
        INotificationHandler<WorkflowExecutionFinished>,
        INotificationHandler<WorkflowStatusChanged>
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly ILogger _logger;

        public PersistWorkflow(IWorkflowInstanceStore workflowInstanceStore, ILogger<PersistWorkflow> logger)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _logger = logger;
        }

        public async Task Handle(WorkflowExecutionPassCompleted notification, CancellationToken cancellationToken)
        {
            var behavior = notification.WorkflowExecutionContext.WorkflowBlueprint.PersistenceBehavior;

            if (behavior == WorkflowPersistenceBehavior.ActivityExecuted || notification.ActivityExecutionContext.ActivityBlueprint.PersistWorkflow)
                await SaveWorkflowAsync(notification.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
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
                await SaveWorkflowAsync(notification.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
            }
        }
        
        public async Task Handle(WorkflowFaulted notification, CancellationToken cancellationToken) => await SaveWorkflowAsync(notification.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
        public async Task Handle(WorkflowStatusChanged notification, CancellationToken cancellationToken) => await SaveWorkflowAsync(notification.WorkflowInstance, cancellationToken); 

        private async ValueTask SaveWorkflowAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Persisting workflow instance {WorkflowInstanceId}", workflowInstance.Id);

            // Can't prune data - we need to figure out a better way to remove activity output data.
            // Doing it right now causes issues when transferring output data from composite activities.
            //workflowExecutionContext.PruneActivityData();
            
            await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);

            _logger.LogDebug("Committed workflow {WorkflowInstanceId} to storage", workflowInstance.Id);
        }
    }
}