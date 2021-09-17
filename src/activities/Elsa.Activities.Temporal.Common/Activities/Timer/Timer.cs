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
    [Trigger(Category = "Timers", Description = "Triggers at a specified interval.")]
    public class Timer : Activity
    {
        private readonly IClock _clock;
        private readonly ILogger _logger;

        public Timer(IClock clock, ILogger<Timer> logger)
        {
            _clock = clock;
            _logger = logger;
        }

        [ActivityInput(Hint = "The time interval at which this activity should tick.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Duration Timeout { get; set; } = default!;

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
            
            ExecuteAt = now.Plus(Timeout);

            if (ExecuteAt <= now)
            {
                _logger.LogDebug("Scheduled trigger time lies in the past ('{Delta}'). Skipping scheduling", now - ExecuteAt);
                context.JournalData.Add("Executed At", now);    
                context.JournalData.Add("Skipped Scheduling", true);
                return Done();
            }
            
            context.JournalData.Add("Scheduled Execution Time", ExecuteAt);
            return Combine(Suspend(), new ScheduleWorkflowResult(ExecuteAt.Value));
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            context.JournalData.Add("Executed At", _clock.GetCurrentInstant());
            return Done();
        }
    }
}