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
        private readonly IClock _clock;

        public CronEventTriggerProvider(IClock clock)
        {
            _clock = clock;
        }

        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<CronEvent> context, CancellationToken cancellationToken)
        {
            var cron = await context.GetActivity<CronEvent>().GetPropertyValueAsync(x => x.CronExpression, cancellationToken);
            var executeAt = GetNextOccurrence(cron);

            return new TimerEventTrigger
            {
                ExecuteAt = executeAt
            };
        }
        
        private Instant GetNextOccurrence(string cronExpression)
        {
            var schedule = CrontabSchedule.Parse(cronExpression);
            var now = _clock.GetCurrentInstant();
            return Instant.FromDateTimeUtc(schedule.GetNextOccurrence(now.ToDateTimeUtc()));
        }
    }
}