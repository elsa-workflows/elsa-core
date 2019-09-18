using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Workflows;
using Elsa.Expressions;
using Elsa.Extensions;
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
            get => GetState(() => new LiteralExpression<TimeSpan>("00:01:00"));
            set => SetState(value);
        }

        public Instant? StartTime
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override async Task<bool> OnCanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            if (StartTime == default)
            {
                StartTime = clock.GetCurrentInstant();
                return true;
            }
            
            return await IsExpiredAsync(context, cancellationToken);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt();
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            StartTime = default;
            return Done();
        }

        private async Task<bool> IsExpiredAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var timeSpan = await expressionEvaluator.EvaluateAsync(TimeoutExpression, context, cancellationToken);
            var now = clock.GetCurrentInstant();
            var startTime = StartTime ?? now;
            var expiresAt = startTime.ToDateTimeUtc() + timeSpan;
            
            return now.ToDateTimeUtc() >= expiresAt;
        }
    }
}