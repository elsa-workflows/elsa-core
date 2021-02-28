﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Services;
using Elsa.Events;
using MediatR;

namespace Elsa.Activities.Temporal.Handlers
{
    public class RemoveScheduledTriggers : INotificationHandler<BlockingActivityRemoved>
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
    }
}