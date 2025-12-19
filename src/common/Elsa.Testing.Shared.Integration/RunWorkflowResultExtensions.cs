using Elsa.Workflows;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides extension methods for <see cref="RunWorkflowResult"/> to facilitate testing with Journal-based assertions.
/// </summary>
[PublicAPI]
public static class RunWorkflowResultExtensions
{
    /// <param name="result">The workflow result.</param>
    extension(RunWorkflowResult result)
    {
        /// <summary>
        /// Gets the activity execution context for the specified activity from the workflow result's journal.
        /// </summary>
        /// <param name="activity">The activity to find.</param>
        /// <returns>The activity execution context, or null if not found.</returns>
        public ActivityExecutionContext? GetActivityContext(IActivity activity) => result.Journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity == activity);

        /// <summary>
        /// Checks if the specified activity was executed (present in the journal).
        /// </summary>
        /// <param name="activity">The activity to check.</param>
        /// <returns>True if the activity was executed, false otherwise.</returns>
        public bool WasExecuted(IActivity activity) => result.GetActivityContext(activity) != null;

        /// <summary>
        /// Checks if the specified activity completed successfully.
        /// </summary>
        /// <param name="activity">The activity to check.</param>
        /// <returns>True if the activity completed, false otherwise.</returns>
        public bool WasCompleted(IActivity activity)
        {
            var context = result.GetActivityContext(activity);
            return context?.Status == ActivityStatus.Completed;
        }

        /// <summary>
        /// Gets the execution status of the specified activity.
        /// </summary>
        /// <param name="activity">The activity to check.</param>
        /// <returns>The activity status, or null if the activity was not executed.</returns>
        public ActivityStatus? GetActivityStatus(IActivity activity) => result.GetActivityContext(activity)?.Status;

        /// <summary>
        /// Gets all executed activities from the workflow result's journal.
        /// </summary>
        /// <returns>Collection of executed activities.</returns>
        public IEnumerable<IActivity> GetExecutedActivities() => result.Journal.ActivityExecutionContexts.Select(x => x.Activity);

        /// <summary>
        /// Gets all completed activities from the workflow result's journal.
        /// </summary>
        /// <returns>Collection of completed activities.</returns>
        public IEnumerable<IActivity> GetCompletedActivities() =>
            result.Journal.ActivityExecutionContexts
                .Where(x => x.Status == ActivityStatus.Completed)
                .Select(x => x.Activity);

        /// <summary>
        /// Counts how many times the specified activity was executed.
        /// Useful for activities that may execute multiple times (e.g., in loops).
        /// </summary>
        /// <param name="activity">The activity to count.</param>
        /// <returns>The number of times the activity was executed.</returns>
        public int GetExecutionCount(IActivity activity) =>
            result.Journal.ActivityExecutionContexts.Count(x => x.Activity == activity);
    }
}
