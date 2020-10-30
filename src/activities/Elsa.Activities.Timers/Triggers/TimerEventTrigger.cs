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
            var duration = await context.GetActivity<TimerEvent>().GetPropertyValueAsync(x => x.Timeout, cancellationToken);
            var now = _clock.GetCurrentInstant();
            var executeAt = now.Plus(duration);

            return new TimerEventTrigger
            {
                ExecuteAt = executeAt
            };
        }
    }
}