using System;
using Elsa.Activities.Temporal.Common.Services;
using NCrontab;

using NodaTime;

namespace Elsa.Activities.Temporal.Hangfire.Services
{
    public class HangfireCrontabParser : ICrontabParser
    {
        public Instant GetNextOccurrence(string cronExpression)
        {
            var schedule = CrontabSchedule.Parse(cronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
           
            return Instant.FromDateTimeUtc(schedule.GetNextOccurrence(DateTime.Now).ToUniversalTime());
        }
    }
}
