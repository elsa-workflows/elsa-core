using Elsa.Activities.Temporal.Common.Services;
using NodaTime;

using Quartz;

namespace Elsa.Activities.Temporal.Quartz.Services
{
    public class QuartzCrontabParser : ICrontabParser
    {
        private readonly IClock _clock;

        public QuartzCrontabParser(IClock clock)
        {
            _clock = clock;
        }

        public Instant GetNextOccurrence(string cronExpression)
        {
            var schedule = new CronExpression(cronExpression);
            var now = _clock.GetCurrentInstant();
            return Instant.FromDateTimeOffset(schedule.GetTimeAfter(now.ToDateTimeOffset())!.Value);
        }
    }
}
