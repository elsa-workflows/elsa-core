using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using NCrontab;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    [ActivityDefinition(
        Category = "Timers",
        Description = "Triggers periodically based on a specified CRON expression.",
        RuntimeDescription =
            "x => !!x.state.cronExpression ? `<strong>${ x.state.cronExpression.expression }</strong>.` : x.definition.description",
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

        private Instant? StartTime
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override bool OnCanExecute(ActivityExecutionContext context) => StartTime == null || IsExpired();
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => OnResume(context);

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            if (IsExpired())
            {
                StartTime = null;
                return Done();
            }

            return Suspend();
        }

        private bool IsExpired()
        {
            var schedule = CrontabSchedule.Parse(CronExpression);
            var now = _clock.GetCurrentInstant();

            StartTime ??= now;

            var nextOccurrence = schedule.GetNextOccurrence(StartTime.Value.ToDateTimeUtc());

            return now.ToDateTimeUtc() >= nextOccurrence;
        }
    }
}