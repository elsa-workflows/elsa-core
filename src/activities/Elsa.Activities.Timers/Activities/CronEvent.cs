using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Scripting;
using Elsa.Services;
using Elsa.Services.Models;
using NCrontab;
using NodaTime;

namespace Elsa.Activities.Timers.Activities
{
    [ActivityDefinition(
        Category = "Timers",
        Description = "Triggers periodically based on a specified CRON expression.",
        RuntimeDescription = "x => !!x.state.cronExpression ? `<strong>${ x.state.cronExpression.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class CronEvent : Activity
    {
        private readonly IClock clock;

        public CronEvent(IClock clock)
        {
            this.clock = clock;
        }

        [ActivityProperty(Hint = "Specify a CRON expression. See https://crontab.guru/ for help.")]
        public IWorkflowExpression<string> CronScriptExpression
        {
            get => GetState<IWorkflowExpression<string>>(() => new LiteralExpression<string>("* * * * *"));
            set => SetState(value);
        }

        public Instant? StartTime
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override async Task<bool> OnCanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            return StartTime == null || await IsExpiredAsync(context, cancellationToken);
        }

        protected override IActivityExecutionResult OnExecute(WorkflowExecutionContext workflowContext)
        {
            return Halt();
        }

        protected override async Task<IActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            if (await IsExpiredAsync(context, cancellationToken))
            {
                StartTime = null;
                return Done();
            }

            return Halt();
        }

        private async Task<bool> IsExpiredAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var cronExpression = await workflowContext.EvaluateAsync(CronScriptExpression, cancellationToken);
            var schedule = CrontabSchedule.Parse(cronExpression);
            var now = clock.GetCurrentInstant();

            if (StartTime == null)
                StartTime = now;

            var nextOccurrence = schedule.GetNextOccurrence(StartTime.Value.ToDateTimeUtc());

            return now.ToDateTimeUtc() >= nextOccurrence;
        }
    }
}