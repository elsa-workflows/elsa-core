using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{
    /// <summary>
    /// Triggers at a specific instant in the future.
    /// </summary>
    [ActivityDefinition(
        Category = "Timers",
        Description = "Triggers at a specified moment in time."
    )]
    public class InstantEvent : Activity
    {
        private readonly IClock _clock;

        public InstantEvent(IClock clock) => _clock = clock;

        /// <summary>
        /// An expression that evaluates to an <see cref="NodaTime.Instant"/>
        /// </summary>
        [ActivityProperty(Hint = "An expression that evaluates to a NodaTime Instant")]
        public Instant Instant { get; set; }

        public Instant? ExecutedAt { get; set; }

        protected override bool OnCanExecute() => ExecutedAt == null || IsExpired();
        protected override IActivityExecutionResult OnExecute() => OnResume();

        protected override IActivityExecutionResult OnResume()
        {
            if (IsExpired())
            {
                ExecutedAt = _clock.GetCurrentInstant();
                return Done();
            }

            return Suspend();
        }

        private bool IsExpired()
        {
            var now = _clock.GetCurrentInstant();
            return now >= Instant;
        }
    }
}