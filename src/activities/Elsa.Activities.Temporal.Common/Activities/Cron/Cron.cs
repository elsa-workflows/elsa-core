using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
{
    [Trigger(
        Category = "Timers",
        Description = "Triggers periodically based on a specified CRON expression.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Cron : Activity
    {
        private readonly IClock _clock;
        private readonly ILogger _logger;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowInstanceScheduler _workflowScheduler;
        private readonly ICrontabParser _crontabParser;

        public Cron(IWorkflowInstanceStore workflowInstanceStore, IWorkflowInstanceScheduler workflowScheduler, ICrontabParser crontabParser, IClock clock, ILogger<Cron> logger)
        {
            _clock = clock;
            _logger = logger;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowScheduler = workflowScheduler;
            _crontabParser = crontabParser;
        }

        [ActivityInput(UIHint = "cron-expression", Hint = "Specify a CRON expression.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string CronExpression { get; set; } = "* * * * *";

        public Instant? ExecuteAt
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var now = _clock.GetCurrentInstant();
            
            if (context.WorkflowExecutionContext.IsFirstPass)
            {
                context.JournalData.Add("Executed At", now);
                context.JournalData.Add("First Pass", true);
                return Done();
            }

            var cancellationToken = context.CancellationToken;
            var workflowInstance = context.WorkflowExecutionContext.WorkflowInstance;
            var executeAt = _crontabParser.GetNextOccurrence(CronExpression);
            
            ExecuteAt = executeAt;

            if (executeAt < now)
            {
                _logger.LogDebug("Scheduled trigger time lies in the past ('{Delta}'). Skipping scheduling", now - ExecuteAt);
                context.JournalData.Add("Executed At", now);    
                context.JournalData.Add("Skipped Scheduling", true);
                return Done();
            }

            // Ensure the workflow instance is persisted so that bookmarks will be updated before scheduling a timer.
            await _workflowInstanceStore.SaveAsync(context.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
            await _workflowScheduler.ScheduleAsync(workflowInstance.Id, Id, executeAt, null, cancellationToken);

            context.JournalData.Add("Scheduled Execution Time", executeAt);
            return Suspend();
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            context.JournalData.Add("Executed At", _clock.GetCurrentInstant());
            return Done();
        }
    }
}