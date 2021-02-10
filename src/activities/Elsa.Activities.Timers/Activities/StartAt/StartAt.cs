using Elsa.Activities.Timers.ActivityResults;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    /// <summary>
    /// Triggers at a specific instant in the future.
    /// </summary>
    [Trigger(
        Category = "Timers",
        Description = "Triggers at a specified moment in time."
    )]
    public class StartAt : Activity
    {
        private readonly IClock _clock;
        private readonly ILogger _logger;

        public StartAt(IClock clock, ILogger<StartAt> logger)
        {
            _clock = clock;
            _logger = logger;
        }

        [ActivityProperty(Hint = "An instant in the future at which this activity should execute.")]
        public Instant Instant { get; set; }

        public Instant? ExecuteAt
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override bool OnCanExecute(ActivityExecutionContext context)
        {
            var executeAt = ExecuteAt;
            var now = _clock.GetCurrentInstant();
            return executeAt == null || executeAt <= now;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (context.WorkflowExecutionContext.IsFirstPass)
                return Done();

            var now = _clock.GetCurrentInstant();
            var executeAt = Instant;

            ExecuteAt = executeAt;

            if (executeAt <= now)
            {
                _logger.LogDebug("Scheduled trigger time lies in the past ('{Delta}'). Skipping scheduling", now - ExecuteAt);
                return Done();
            }

            return Combine(Suspend(), new ScheduleWorkflowResult(executeAt));
        }

        protected override IActivityExecutionResult OnResume() => Done();
    }
}