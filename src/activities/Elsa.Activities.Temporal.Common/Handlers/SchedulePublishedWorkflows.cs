using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Events;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Temporal.Common.Handlers
{
    public class SchedulePublishedWorkflows : INotificationHandler<TriggerIndexingFinished>
    {
        // TODO: Design multi-tenancy. 
        private const string? TenantId = default;

        protected readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SchedulePublishedWorkflows> _logger;

        public SchedulePublishedWorkflows(ILogger<SchedulePublishedWorkflows> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public virtual async Task Handle(TriggerIndexingFinished notification, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            await SchedulePublishedWorkflowsInternalAsync(scope.ServiceProvider, cancellationToken);
        }

        protected async Task SchedulePublishedWorkflowsInternalAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var triggerFinder = serviceProvider.GetRequiredService<ITriggerFinder>();
            var workflowScheduler = serviceProvider.GetRequiredService<IWorkflowDefinitionScheduler>();

            var startAtTriggers = await triggerFinder.FindTriggersAsync<StartAt>(TenantId, cancellationToken);
            var timerTriggers = await triggerFinder.FindTriggersAsync<Timer>(TenantId, cancellationToken);
            var cronTriggers = await triggerFinder.FindTriggersAsync<Cron>(TenantId, cancellationToken);

            await workflowScheduler.UnscheduleAllAsync(cancellationToken);

            foreach (var trigger in startAtTriggers)
            {
                var bookmark = (StartAtBookmark)trigger.Bookmark;
                await Try(() => workflowScheduler.ScheduleAsync(trigger.WorkflowBlueprint.Id, trigger.ActivityId, bookmark.ExecuteAt, null, cancellationToken));
            }

            foreach (var trigger in timerTriggers)
            {
                var bookmark = (TimerBookmark)trigger.Bookmark;
                await Try(() => workflowScheduler.ScheduleAsync(trigger.WorkflowBlueprint.Id, trigger.ActivityId, bookmark.ExecuteAt, bookmark.Interval, cancellationToken));
            }

            foreach (var trigger in cronTriggers)
            {
                var bookmark = (CronBookmark)trigger.Bookmark;
                await Try(() => workflowScheduler.ScheduleAsync(trigger.WorkflowBlueprint.Id, trigger.ActivityId, bookmark.CronExpression, cancellationToken));
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