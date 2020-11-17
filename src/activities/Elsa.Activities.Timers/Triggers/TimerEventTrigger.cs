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
        public override ITrigger GetTrigger(TriggerProviderContext<TimerEvent> context)
        {
            var executeAt = context.GetActivity<TimerEvent>().GetState(x => x.ExecuteAt);

            return new TimerEventTrigger
            {
                ExecuteAt = executeAt
            };
        }
    }
}