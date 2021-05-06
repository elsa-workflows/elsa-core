using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Events;
using MediatR;

namespace Elsa.Activities.Temporal.Common.Handlers
{
    public class RemoveScheduledTriggers : INotificationHandler<BlockingActivityRemoved>, INotificationHandler<WorkflowDefinitionPublished>, INotificationHandler<WorkflowDefinitionRetracted>
    {
        private readonly string[] _supportedTypes = { nameof(Timer), nameof(StartAt), nameof(Cron) };
        private readonly IWorkflowScheduler _workflowScheduler;
        public RemoveScheduledTriggers(IWorkflowScheduler workflowScheduler) => _workflowScheduler = workflowScheduler;

        public async Task Handle(BlockingActivityRemoved notification, CancellationToken cancellationToken)
        {
            if (!_supportedTypes.Contains(notification.BlockingActivity.ActivityType))
                return;

            await _workflowScheduler.UnscheduleWorkflowAsync(
                null,
                notification.WorkflowExecutionContext.WorkflowInstance.Id,
                notification.BlockingActivity.ActivityId,
                notification.WorkflowExecutionContext.WorkflowInstance.TenantId,
                cancellationToken);
        }

        public Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => _workflowScheduler.UnscheduleWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, notification.WorkflowDefinition.TenantId, cancellationToken);
        public Task Handle(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => _workflowScheduler.UnscheduleWorkflowDefinitionAsync(notification.WorkflowDefinition.DefinitionId, notification.WorkflowDefinition.TenantId, cancellationToken);
    }
}