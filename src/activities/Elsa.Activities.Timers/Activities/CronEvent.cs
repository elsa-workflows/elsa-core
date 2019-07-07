using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NCrontab;
using NodaTime;

namespace Elsa.Activities.Timers.Activities
{
    public class CronEvent : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IClock clock;

        public CronEvent(IWorkflowExpressionEvaluator expressionEvaluator, IClock clock)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.clock = clock;
        }

        public WorkflowExpression<string> CronExpression
        {
            get => GetState(() => new PlainTextExpression("* * * * *"));
            set => SetState(value);
        }

        public Instant? StartTime
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext workflowContext)
        {
            if (StartTime == null)
            {
                StartTime = clock.GetCurrentInstant();
            }
            
            return Halt();
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(workflowContext, cancellationToken);

            return isExpired ? Done() : Halt();
        }

        private async Task<bool> IsExpiredAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var cronExpression = await expressionEvaluator.EvaluateAsync(CronExpression, workflowContext, cancellationToken);
            var schedule = CrontabSchedule.Parse(cronExpression);
            var now = clock.GetCurrentInstant();
            var startTime = StartTime ?? now;
            var nextOccurrence = schedule.GetNextOccurrence(startTime.ToDateTimeUtc());

            return now.ToDateTimeUtc() >= nextOccurrence;
        }
    }
}