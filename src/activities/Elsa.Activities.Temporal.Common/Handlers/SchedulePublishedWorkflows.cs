using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Events;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Temporal.Common.Handlers
{
    public class SchedulePublishedWorkflows : INotificationHandler<TriggerIndexingFinished>
    {
        // TODO: Design multi-tenancy. 
        private const string? TenantId = default;

        private readonly IWorkflowDefinitionScheduler _workflowScheduler;
        private readonly ITriggerFinder _triggerFinder;
        private readonly ILogger<SchedulePublishedWorkflows> _logger;

        public SchedulePublishedWorkflows(IWorkflowDefinitionScheduler workflowScheduler, ITriggerFinder triggerFinder, ILogger<SchedulePublishedWorkflows> logger)
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

            await _workflowScheduler.UnscheduleAllAsync(cancellationToken);

            foreach (var trigger in startAtTriggers)
            {
                var bookmark = (StartAtBookmark) trigger.Bookmark;
                await Try(() => _workflowScheduler.ScheduleAsync(trigger.WorkflowBlueprint.Id, trigger.ActivityId, bookmark.ExecuteAt, null, cancellationToken));
            }

            foreach (var trigger in timerTriggers)
            {
                var bookmark = (TimerBookmark) trigger.Bookmark;
                await Try(() => _workflowScheduler.ScheduleAsync(trigger.WorkflowBlueprint.Id, trigger.ActivityId, bookmark.ExecuteAt, bookmark.Interval, cancellationToken));
            }

            foreach (var trigger in cronTriggers)
            {
                var bookmark = (CronBookmark) trigger.Bookmark;
                await Try(() => _workflowScheduler.ScheduleAsync(trigger.WorkflowBlueprint.Id, trigger.ActivityId, bookmark.CronExpression, cancellationToken));
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