using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Events;
using Elsa.Triggers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Temporal.Common.Handlers
{
    public class SchedulePublishedWorkflows : INotificationHandler<TriggerIndexingFinished>
    {
        // TODO: Design multi-tenancy. 
        private const string? TenantId = default;

        private readonly IWorkflowScheduler _workflowScheduler;
        private readonly ITriggerFinder _triggerFinder;
        private readonly ILogger<SchedulePublishedWorkflows> _logger;

        public SchedulePublishedWorkflows(IWorkflowScheduler workflowScheduler, ITriggerFinder triggerFinder, ILogger<SchedulePublishedWorkflows> logger)
        {
            _workflowScheduler = workflowScheduler;
            _triggerFinder = triggerFinder;
            _logger = logger;
        }

        public async Task Handle(TriggerIndexingFinished notification, CancellationToken cancellationToken)
        {
            var startAtTriggers = await _triggerFinder.FindTriggersAsync<StartAt>(TenantId, cancellationToken);
            var timerTriggers = await _triggerFinder.FindTriggersAsync<Timer>(TenantId, cancellationToken);
            var cronTriggers = await _triggerFinder.FindTriggersAsync<Cron>(TenantId, cancellationToken);

            foreach (var trigger in startAtTriggers)
            {
                var bookmark = (StartAtBookmark) trigger.Bookmark;
                await Try(() => _workflowScheduler.ScheduleWorkflowAsync(trigger.WorkflowBlueprint.Id, null, trigger.ActivityId, TenantId, bookmark.ExecuteAt, null, cancellationToken));
            }

            foreach (var trigger in timerTriggers)
            {
                var bookmark = (TimerBookmark) trigger.Bookmark;
                await Try(() => _workflowScheduler.ScheduleWorkflowAsync(trigger.WorkflowBlueprint.Id, null, trigger.ActivityId, TenantId, bookmark.ExecuteAt, bookmark.Interval, cancellationToken));
            }

            foreach (var trigger in cronTriggers)
            {
                var bookmark = (CronBookmark) trigger.Bookmark;
                await Try(() => _workflowScheduler.ScheduleWorkflowAsync(trigger.WorkflowBlueprint.Id, null, trigger.ActivityId, TenantId, bookmark.CronExpression, cancellationToken));
            }
        }

        private async Task Try(Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can't schedule workflow");
            }
        }
    }
}