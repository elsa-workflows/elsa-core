using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Activities.Timers.Activities
{
    [ActivityDefinition(
        Category = "Timers",
        Description = "Triggers at a specified interval."
    )]
    public class TimerEvent : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IClock clock;

        public TimerEvent(IWorkflowExpressionEvaluator expressionEvaluator, IClock clock)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.clock = clock;
        }

        [ActivityProperty(Hint = "An expression that evaluates to a TimeSpan value")]
        public WorkflowExpression<TimeSpan> TimeoutExpression
        {
            get => GetState(() => new WorkflowExpression<TimeSpan>(LiteralEvaluator.SyntaxName, "00:01:00"));
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

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt();
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            if (await IsExpiredAsync(context, cancellationToken))
            {
                StartTime = null;
                return Done();
            }
            
            return Halt();
        }

        private async Task<bool> IsExpiredAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var now = clock.GetCurrentInstant();

            StartTime ??= now;
            
            var timeSpan = await expressionEvaluator.EvaluateAsync(TimeoutExpression, context, cancellationToken);
            var expiresAt = StartTime.Value.ToDateTimeUtc() + timeSpan;
            
            return now.ToDateTimeUtc() >= expiresAt;
        }
    }
}