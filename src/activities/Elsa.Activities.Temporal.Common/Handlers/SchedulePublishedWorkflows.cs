using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Events;
using Elsa.Triggers;
using MediatR;

namespace Elsa.Activities.Temporal.Common.Handlers
{
    public class SchedulePublishedWorkflows : INotificationHandler<TriggerIndexingFinished>
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;

        private readonly IWorkflowScheduler _workflowScheduler;
        private readonly ITriggerFinder _triggerFinder;

        public SchedulePublishedWorkflows(IWorkflowScheduler workflowScheduler, ITriggerFinder triggerFinder)
        {
            _workflowScheduler = workflowScheduler;
            _triggerFinder = triggerFinder;
        }

        public async Task Handle(TriggerIndexingFinished notification, CancellationToken cancellationToken)
        {
            var startAtTriggers = await _triggerFinder.FindTriggersAsync<StartAt>(TenantId, cancellationToken);
            var timerTriggers = await _triggerFinder.FindTriggersAsync<Timer>(TenantId, cancellationToken);
            var cronTriggers = await _triggerFinder.FindTriggersAsync<Cron>(TenantId, cancellationToken);

            foreach (var trigger in startAtTriggers)
            {
                var bookmark = (StartAtBookmark) trigger.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(trigger.WorkflowBlueprint.Id, null, trigger.ActivityId, TenantId, bookmark.ExecuteAt, null, cancellationToken);
            }

            foreach (var trigger in timerTriggers)
            {
                var bookmark = (TimerBookmark) trigger.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(trigger.WorkflowBlueprint.Id, null, trigger.ActivityId, TenantId, bookmark.ExecuteAt, bookmark.Interval, cancellationToken);
            }

            foreach (var trigger in cronTriggers)
            {
                var bookmark = (CronBookmark) trigger.Bookmark;
                await _workflowScheduler.ScheduleWorkflowAsync(trigger.WorkflowBlueprint.Id, null, trigger.ActivityId, TenantId, bookmark.CronExpression, cancellationToken);
            }
        }
    }
}