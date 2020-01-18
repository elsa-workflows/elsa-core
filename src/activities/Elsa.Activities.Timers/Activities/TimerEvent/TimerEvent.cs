using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    [ActivityDefinition(
        Category = "Timers",
        Description = "Triggers at a specified interval."
    )]
    public class TimerEvent : Activity
    {
        private readonly IClock clock;

        public TimerEvent(IClock clock)
        {
            this.clock = clock;
        }

        [ActivityProperty(Hint = "An expression that evaluates to a Duration value")]
        public IWorkflowExpression<Duration> Timeout
        {
            get => GetState<IWorkflowExpression<Duration>>(() => new LiteralExpression<Duration>("00:01:00"));
            set => SetState(value);
        }

        public Instant? StartTime
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override async Task<bool> OnCanExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            return StartTime == null || await IsExpiredAsync(context, cancellationToken);
        }

        protected override Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => ExecuteInternalAsync(context, cancellationToken);
        protected override Task<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => ExecuteInternalAsync(context, cancellationToken);

        private async Task<IActivityExecutionResult> ExecuteInternalAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            if (await IsExpiredAsync(context, cancellationToken))
            {
                StartTime = null;
                return Done();
            }
            
            return Suspend();
        }

        private async Task<bool> IsExpiredAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var now = clock.GetCurrentInstant();

            if (StartTime == null)
                StartTime = now;

            var startTime = StartTime.Value;
            var timeout = await context.EvaluateAsync(Timeout, cancellationToken);
            var expiresAt = startTime + timeout - Duration.FromMilliseconds(1000);
            
            return now >= expiresAt;
        }
    }
}