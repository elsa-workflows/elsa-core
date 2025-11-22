using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Adds extension methods to <see cref="ActivityExecutionContext"/>.
/// </summary>
public static class BackgroundActivityExecutionContextExtensions
{
    /// <summary>
    /// A key into the activity execution context's transient properties that indicates whether the current activity is being executed in the background.
    /// </summary>
    public static readonly object IsBackgroundExecution = new();

    /// <param name="activityExecutionContext"></param>
    extension(ActivityExecutionContext activityExecutionContext)
    {
        /// <summary>
        /// Configures the activity execution context to execute the current activity in the background.
        /// </summary>
        public void SetIsBackgroundExecution(bool value = true)
        {
            activityExecutionContext.TransientProperties[IsBackgroundExecution] = value;
        }

        /// <summary>
        /// Gets a value indicating whether the current activity is being executed in the background.
        /// </summary>
        public bool GetIsBackgroundExecution()
        {
            return activityExecutionContext.TransientProperties.ContainsKey(IsBackgroundExecution);
        }

        /// <summary>
        /// Sets the background outcomes.
        /// </summary>
        public void SetBackgroundOutcomes(IEnumerable<string> outcomes)
        {
            var outcomesList = outcomes.ToList();
            activityExecutionContext.SetProperty("BackgroundOutcomes", outcomesList);
        }

        /// <summary>
        /// Gets the background outcomes.
        /// </summary>
        public IEnumerable<string>? GetBackgroundOutcomes()
        {
            return activityExecutionContext.GetProperty<IEnumerable<string>>("BackgroundOutcomes");
        }

        /// <summary>
        /// Sets the background outcomes.
        /// </summary>
        public void SetBackgroundCompletion()
        {
            activityExecutionContext.SetProperty("BackgroundCompletion", true);
        }

        /// <summary>
        /// Gets the background outcomes.
        /// </summary>
        public bool? GetBackgroundCompleted()
        {
            return activityExecutionContext.GetProperty<bool>("BackgroundCompletion");
        }

        /// <summary>
        /// Sets the background scheduled activities.
        /// </summary>
        public void SetBackgroundScheduledActivities(IEnumerable<ScheduledActivity> scheduledActivities)
        {
            var scheduledActivitiesList = scheduledActivities.ToList();
            activityExecutionContext.SetProperty("BackgroundScheduledActivities", scheduledActivitiesList);
        }

        /// <summary>
        /// Gets the background scheduled activities.
        /// </summary>
        public IEnumerable<ScheduledActivity> GetBackgroundScheduledActivities()
        {
            return activityExecutionContext.GetProperty<IEnumerable<ScheduledActivity>>("BackgroundScheduledActivities") ?? [];
        }
    }
}