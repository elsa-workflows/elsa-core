using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
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
        private readonly IClock clock;

        public TimerEvent(IClock clock)
        {
            this.clock = clock;
        }

        [ActivityProperty(Hint = "An expression that evaluates to a TimeSpan value")]
        public IWorkflowExpression<TimeSpan> Timeout
        {
            get => GetState<IWorkflowExpression<TimeSpan>>(() => new LiteralExpression<TimeSpan>("00:01:00"));
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

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Suspend();

        protected override async Task<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
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
            
            var timeSpan = await context.EvaluateAsync(Timeout, cancellationToken);
            var expiresAt = StartTime.Value.ToDateTimeUtc() + timeSpan;
            
            return now.ToDateTimeUtc() >= expiresAt;
        }
    }
}