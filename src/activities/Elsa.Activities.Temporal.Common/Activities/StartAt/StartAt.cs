using Elsa.Activities.Temporal.Common.ActivityResults;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
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

        [ActivityInput(Hint = "An instant in the future at which this activity should execute.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Instant Instant { get; set; }

        public Instant? ExecuteAt
        {
            get => GetState<Instant?>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var now = _clock.GetCurrentInstant();
            
            if (context.WorkflowExecutionContext.IsFirstPass)
            {
                context.JournalData.Add("Executed At", now);
                context.JournalData.Add("First Pass", true);
                return Done();
            }
            
            var executeAt = Instant;

            ExecuteAt = executeAt;

            if (executeAt <= now)
            {
                _logger.LogDebug("Scheduled trigger time lies in the past ('{Delta}'). Skipping scheduling", now - ExecuteAt);
                context.JournalData.Add("Executed At", now);    
                context.JournalData.Add("Skipped Scheduling", true);
                return Done();
            }

            context.JournalData.Add("Scheduled Execution Time", executeAt);
            return Combine(Suspend(), new ScheduleWorkflowResult(executeAt));
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            context.JournalData.Add("Executed At", _clock.GetCurrentInstant());
            return Done();
        }
    }
}