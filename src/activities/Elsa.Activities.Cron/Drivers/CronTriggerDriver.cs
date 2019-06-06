using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Cron.Activities;
using Elsa.Core.Handlers;
using Elsa.Models;
using Elsa.Results;
using NCrontab;
using NodaTime;

namespace Elsa.Activities.Cron.Drivers
{
    public class CronTriggerDriver : ActivityDriver<CronTrigger>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IClock clock;

        public CronTriggerDriver(IWorkflowExpressionEvaluator expressionEvaluator, IClock clock)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.clock = clock;
        }

        protected override async Task<bool> OnCanExecuteAsync(CronTrigger activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            return activity.StartTimestamp == null || await IsExpiredAsync(activity, workflowContext, cancellationToken);
        }

        protected override ActivityExecutionResult OnExecute(CronTrigger activity, WorkflowExecutionContext workflowContext)
        {
            if (activity.StartTimestamp == null)
                activity.StartTimestamp = clock.GetCurrentInstant().ToDateTimeUtc();

            return Halt();
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(CronTrigger activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var isExpired = await IsExpiredAsync(activity, workflowContext, cancellationToken);

            return isExpired ? (ActivityExecutionResult) Endpoint(EndpointNames.Done) : Halt();
        }

        private async Task<bool> IsExpiredAsync(CronTrigger activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var cronExpression = await expressionEvaluator.EvaluateAsync(activity.CronExpression, workflowContext, cancellationToken);
            var schedule = CrontabSchedule.Parse(cronExpression);
            var now = clock.GetCurrentInstant().ToDateTimeUtc();
            var startTimestamp = activity.StartTimestamp ?? now;
            var nextOccurrence = schedule.GetNextOccurrence(startTimestamp);

            return now >= nextOccurrence;
        }
    }
}