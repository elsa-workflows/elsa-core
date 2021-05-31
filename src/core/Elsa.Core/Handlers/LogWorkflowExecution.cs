using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Handlers
{
    public class LogWorkflowExecution : INotificationHandler<ActivityExecuting>, INotificationHandler<ActivityExecuted>, INotificationHandler<WorkflowExecutionBurstCompleted>, INotificationHandler<ActivityFaulted>
    {
        private readonly ILogger _logger;

        public LogWorkflowExecution(ILogger<LogWorkflowExecution> logger)
        {
            _logger = logger;
        }

        public Task Handle(ActivityExecuting notification, CancellationToken cancellationToken)
        {
            var activityBlueprint = notification.ActivityBlueprint;
            var activityId = activityBlueprint.Id;
            var workflowInstanceId = notification.WorkflowExecutionContext.WorkflowInstance.Id;
            _logger.LogDebug("Executing activity {ActivityType} {ActivityId} for workflow {WorkflowInstanceId}", activityBlueprint.Type, activityId, workflowInstanceId);
            return Task.CompletedTask;
        }

        public Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            var activityBlueprint = notification.ActivityBlueprint;
            var activityId = activityBlueprint.Id;
            var workflowInstanceId = notification.WorkflowExecutionContext.WorkflowInstance.Id;
            _logger.LogDebug("Executed activity {ActivityType} {ActivityId} for workflow {WorkflowInstanceId}", activityBlueprint.Type, activityId, workflowInstanceId);
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowExecutionBurstCompleted notification, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Burst of workflow execution completed for workflow {WorkflowInstanceId}", notification.WorkflowExecutionContext.WorkflowInstance.Id);
            return Task.CompletedTask;
        }

        public Task Handle(ActivityFaulted notification, CancellationToken cancellationToken)
        {
            _logger.LogWarning(notification.Exception, "An error occurred while executing activity {ActivityId}. Entering Faulted state", notification.ActivityBlueprint.Id);
            return Task.CompletedTask;
        }
    }
}