namespace Elsa.Workflows;

/// <summary>
/// Adds extension methods to <see cref="ActivityExecutionContext"/>.
/// </summary>
public static class BackgroundActivityExecutionContextExtensions
{
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
    public static IEnumerable<string> GetBackgroundOutcomes(this ActivityExecutionContext activityExecutionContext)
    {
        return activityExecutionContext.GetProperty<IEnumerable<string>>("BackgroundOutcomes") ?? Enumerable.Empty<string>();
    }
}