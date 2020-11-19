using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using NCrontab;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    [Trigger(
        Category = "Timers",
        Description = "Triggers periodically based on a specified CRON expression.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class CronEvent : Activity
    {
        private readonly IClock _clock;

        public CronEvent(IClock clock)
        {
            _clock = clock;
        }
        
        
        [ActivityProperty(Hint = "Specify a CRON expression. See https://crontab.guru/ for help.")]
        public string CronExpression { get; set; } = "* * * * *";
        
        public Instant ExecuteAt
        {
            get => GetState<Instant>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            ExecuteAt = GetNextOccurrence(CronExpression);
            return Suspend();
        }

        protected override IActivityExecutionResult OnResume() => Done();
        
        private Instant GetNextOccurrence(string cronExpression)
        {
            var schedule = CrontabSchedule.Parse(cronExpression);
            var now = _clock.GetCurrentInstant();
            return Instant.FromDateTimeUtc(schedule.GetNextOccurrence(now.ToDateTimeUtc()));
        }
    }
}