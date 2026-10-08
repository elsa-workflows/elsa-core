using Elsa.Workflows;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides extension methods for <see cref="ActivityExecutionContext"/> to enhance its functionality.
/// </summary>
public static class ActivityExecutionContextExtensions
{
    /// <param name="activityExecutionContext">The activity execution context instance used for checking the scheduled activity.</param>
    extension(ActivityExecutionContext activityExecutionContext)
    {
        /// <summary>
        /// Determines whether the specified activity is scheduled in the workflow execution context.
        /// </summary>
        /// <param name="activity">The activity to check for scheduling.</param>
        /// <returns>True if the specified activity is scheduled; otherwise, false.</returns>
        public bool HasScheduledActivity(IActivity activity)
        {
            return activityExecutionContext.WorkflowExecutionContext.Scheduler.Find(x => x.Activity == activity) != null;
        }

        /// <summary>
        /// Retrieves the collection of outcomes recorded in the execution context's journal data.
        /// </summary>
        /// <returns>A collection of outcome strings, or an empty collection if no outcomes are present.</returns>
        public IEnumerable<string> GetOutcomes()
        {
            return activityExecutionContext.JournalData.TryGetValue("Outcomes", out var outcomes) && outcomes is string[] arr ? arr : [];
        }

        /// <summary>
        /// Determines whether the specified outcome exists in the current activity execution context.
        /// </summary>
        /// <param name="outcome">The outcome to verify within the execution context.</param>
        /// <returns>True if the specified outcome exists; otherwise, false.</returns>
        public bool HasOutcome(string outcome)
        {
            return activityExecutionContext.GetOutcomes().Contains(outcome);
        }
    }
}