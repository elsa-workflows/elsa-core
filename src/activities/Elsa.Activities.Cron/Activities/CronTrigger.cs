using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NCrontab;
using NodaTime;

namespace Elsa.Activities.Cron.Activities
{
    public class CronTrigger : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IClock clock;

        public CronTrigger(IWorkflowExpressionEvaluator expressionEvaluator, IClock clock)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.clock = clock;
        }

        public WorkflowExpression<string> CronExpression
        {
            get => GetState(() => new PlainTextExpression("* * * * *"));
            set => SetState(value);
        }

        public Instant? StartTimestamp
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext workflowContext)
        {
            if (StartTimestamp == null)
            {
                StartTimestamp = clock.GetCurrentInstant();
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
            var startTimestamp = StartTimestamp ?? now;
            var nextOccurrence = schedule.GetNextOccurrence(startTimestamp.ToDateTimeUtc());

            return now.ToDateTimeUtc() >= nextOccurrence;
        }
    }
}