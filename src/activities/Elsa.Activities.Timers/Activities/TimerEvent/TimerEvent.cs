using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    [Trigger(Category = "Timers", Description = "Triggers at a specified interval.")]
    public class TimerEvent : Activity
    {
        private readonly IClock _clock;

        public TimerEvent(IClock clock)
        {
            _clock = clock;
        }
        
        [ActivityProperty(Hint = "An expression that evaluates to a Duration value.")]
        public Duration Timeout { get; set; } = default!;

        public Instant? ExecuteAt
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            ExecuteAt = _clock.GetCurrentInstant().Plus(Timeout);
            return Suspend();
        }

        protected override IActivityExecutionResult OnResume() => Done();
    }
}