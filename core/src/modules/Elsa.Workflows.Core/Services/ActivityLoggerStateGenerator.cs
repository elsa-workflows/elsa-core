namespace Elsa.Workflows.Services;

/// <summary>
/// Generates logger state for the specified <see cref="ActivityExecutionContext"/>.
/// </summary>
public class ActivityLoggerStateGenerator : ILoggerStateGenerator<ActivityExecutionContext>
{
    /// <summary>
    /// Generates logger state for the specified <see cref="ActivityExecutionContext"/>.
    /// </summary>
    /// <param name="activityExecutionContext">The <see cref="ActivityExecutionContext"/> to generate logger state for.</param>
    /// <returns>A <see cref="Dictionary{String, Object}" /> containing the state related to the <see cref="ActivityExecutionContext"/>.</returns>
    public Dictionary<string, object> GenerateLoggerState(ActivityExecutionContext activityExecutionContext)
    {
        return new()
        {
            ["ActivityInstanceId"] = activityExecutionContext.Id
        };
    }
}
