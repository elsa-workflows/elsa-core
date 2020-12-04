using Elsa.Activities.Timers.ActivityResults;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    [Trigger(Category = "Timers", Description = "Triggers at a specified interval.")]
    public class Timer : Activity
    {
        private readonly IClock _clock;

        public Timer(IClock clock)
        {
            _clock = clock;
        }

        [ActivityProperty(Hint = "The time interval at which this activity should tick.")]
        public Duration Timeout { get; set; } = default!;

        public Instant? ExecuteAt
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (context.WorkflowExecutionContext.IsFirstPass)
                return Done();
            
            ExecuteAt = _clock.GetCurrentInstant().Plus(Timeout);

            return Combine(Suspend(), new ScheduleWorkflowResult(ExecuteAt.Value));
        }

        protected override IActivityExecutionResult OnResume() => Done();
    }
}