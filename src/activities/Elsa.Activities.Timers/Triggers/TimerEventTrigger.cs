using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class TimerEventTrigger : Trigger
    {
        public Instant ExecuteAt { get; set; }
    }

    public class TimerEventTriggerProvider : TriggerProvider<TimerEventTrigger, TimerEvent>
    {
        private readonly IClock _clock;

        public TimerEventTriggerProvider(IClock clock)
        {
            _clock = clock;
        }

        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<TimerEvent> context, CancellationToken cancellationToken)
        {
            var activity = context.GetActivity<TimerEvent>();
            var executeAt = activity.GetState(x => x.ExecuteAt);

            if (executeAt == null)
            {
                var timeout = await activity.GetPropertyValueAsync(x => x.Timeout, cancellationToken); 
                executeAt = _clock.GetCurrentInstant().Plus(timeout);
            }

            return new TimerEventTrigger
            {
                ExecuteAt = executeAt.Value
            };
        }
    }
}