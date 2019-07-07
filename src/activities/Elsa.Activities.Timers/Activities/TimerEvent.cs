using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Activities.Timers.Activities
{
    public class TimerEvent : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IClock clock;

        public TimerEvent(IWorkflowExpressionEvaluator expressionEvaluator, IClock clock)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.clock = clock;
        }

        public WorkflowExpression<TimeSpan> TimeoutExpression
        {
            get => GetState(() => new PlainTextExpression<TimeSpan>("00:01:00"));
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
            var timeSpan = await expressionEvaluator.EvaluateAsync(TimeoutExpression, workflowContext, cancellationToken);
            var now = clock.GetCurrentInstant();
            var startTime = StartTime ?? now;
            var expiresAt = startTime.ToDateTimeUtc() + timeSpan;
            
            return now.ToDateTimeUtc() >= expiresAt;
        }
    }
}