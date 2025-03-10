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
    
    /// <summary>
    /// Configures the activity execution context to execute the current activity in the background.
    /// </summary>
    public static void SetIsBackgroundExecution(this ActivityExecutionContext activityExecutionContext, bool value = true)
    {
        activityExecutionContext.TransientProperties[IsBackgroundExecution] = value;
    }
    
    /// <summary>
    /// Gets a value indicating whether the current activity is being executed in the background.
    /// </summary>
    public static bool GetIsBackgroundExecution(this ActivityExecutionContext activityExecutionContext)
    {
        return activityExecutionContext.TransientProperties.ContainsKey(IsBackgroundExecution);
    }
    
    /// <summary>
    /// Sets the background outcomes.
    /// </summary>
    public static void SetBackgroundOutcomes(this ActivityExecutionContext activityExecutionContext, IEnumerable<string> outcomes)
    {
        var outcomesList = outcomes.ToList();
        activityExecutionContext.SetProperty("BackgroundOutcomes", outcomesList);
    }
    
    /// <summary>
    /// Gets the background outcomes.
    /// </summary>
    public static IEnumerable<string>? GetBackgroundOutcomes(this ActivityExecutionContext activityExecutionContext)
    {
        return activityExecutionContext.GetProperty<IEnumerable<string>>("BackgroundOutcomes");
    }
    
    /// <summary>
    /// Sets the background outcomes.
    /// </summary>
    public static void SetBackgroundCompletion(this ActivityExecutionContext activityExecutionContext)
    {
        activityExecutionContext.SetProperty("BackgroundCompletion", true);
    }
    
    /// <summary>
    /// Gets the background outcomes.
    /// </summary>
    public static bool? GetBackgroundCompleted(this ActivityExecutionContext activityExecutionContext)
    {
        return activityExecutionContext.GetProperty<bool>("BackgroundCompletion");
    }
    
    /// <summary>
    /// Sets the background scheduled activities.
    /// </summary>
    public static void SetBackgroundScheduledActivities(this ActivityExecutionContext activityExecutionContext, IEnumerable<ScheduledActivity> scheduledActivities)
    {
        var scheduledActivitiesList = scheduledActivities.ToList();
        activityExecutionContext.SetProperty("BackgroundScheduledActivities", scheduledActivitiesList);
    }
    
    /// <summary>
    /// Gets the background scheduled activities.
    /// </summary>
    /// <param name="activityExecutionContext"></param>
    public static IEnumerable<ScheduledActivity> GetBackgroundScheduledActivities(this ActivityExecutionContext activityExecutionContext)
    {
        return activityExecutionContext.GetProperty<IEnumerable<ScheduledActivity>>("BackgroundScheduledActivities") ?? [];
    }
}