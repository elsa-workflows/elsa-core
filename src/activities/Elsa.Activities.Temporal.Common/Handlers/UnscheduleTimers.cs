using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Events;
using MediatR;

namespace Elsa.Activities.Temporal.Common.Handlers
{
    public class UnscheduleTimers :
        INotificationHandler<BlockingActivityRemoved>,
        INotificationHandler<BookmarksDeleted>,
        INotificationHandler<TriggersDeleted>
    {
        private readonly IWorkflowDefinitionScheduler _workflowDefinitionScheduler;
        private readonly IWorkflowInstanceScheduler _workflowInstanceScheduler;

        public UnscheduleTimers(IWorkflowDefinitionScheduler workflowDefinitionScheduler, IWorkflowInstanceScheduler workflowInstanceScheduler)
        {
            _workflowDefinitionScheduler = workflowDefinitionScheduler;
            _workflowInstanceScheduler = workflowInstanceScheduler;
        }

        public async Task Handle(BlockingActivityRemoved notification, CancellationToken cancellationToken)
        {
            // TODO: Consider introducing a "stereotype" field for activities to exit early in case they are not stereotyped as "temporal".

            await _workflowInstanceScheduler.UnscheduleAsync(
                notification.WorkflowExecutionContext.WorkflowInstance.Id,
                notification.BlockingActivity.ActivityId,
                cancellationToken);
        }

        public Task Handle(BookmarksDeleted notification, CancellationToken cancellationToken) => _workflowInstanceScheduler.UnscheduleAsync(notification.WorkflowInstanceId, cancellationToken);

        public Task Handle(TriggersDeleted notification, CancellationToken cancellationToken) => _workflowDefinitionScheduler.UnscheduleAsync(notification.WorkflowDefinitionId, cancellationToken);
    }
}