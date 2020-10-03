using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
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
        public Duration Timeout { get; set; } = default!;

        private Instant? StartTime { get; set; }

        protected override bool OnCanExecute() => StartTime == null || IsExpired();
        protected override IActivityExecutionResult OnExecute() => ExecuteInternal();
        protected override IActivityExecutionResult OnResume() => ExecuteInternal();

        private IActivityExecutionResult ExecuteInternal()
        {
            if (IsExpired())
            {
                StartTime = null;
                return Done();
            }

            return Suspend();
        }

        private bool IsExpired()
        {
            var now = clock.GetCurrentInstant();

            StartTime ??= now;

            var startTime = StartTime.Value;
            var expiresAt = startTime + Timeout;

            return now >= expiresAt;
        }
    }
}