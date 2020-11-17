using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;
using NCrontab;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class CronEventTrigger : Trigger
    {
        public Instant ExecuteAt { get; set; }
    }

    public class CronEventTriggerProvider : TriggerProvider<CronEventTrigger, CronEvent>
    {
        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<CronEvent> context, CancellationToken cancellationToken)
        {
            var executeAt = context.GetActivity<CronEvent>().GetState(x => x.ExecuteAt);

            return new TimerEventTrigger
            {
                ExecuteAt = executeAt
            };
        }
    }
}