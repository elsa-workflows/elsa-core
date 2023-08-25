namespace Elsa.Workflows.Core;

/// <summary>
/// Provides extension methods for <see cref="ActivityExecutionContext"/>. 
/// </summary>
public static class BreakBehaviorActivityExecutionContextExtensions
{
    /// <summary>
    /// Gets a value indicating whether the current activity is breaking out of a loop.
    /// </summary>
    public static bool GetIsBreaking(this ActivityExecutionContext context) => context.GetProperty<bool>("IsBreaking");
    
    /// <summary>
    /// Sets a value indicating whether the current activity is breaking out of a loop.
    /// </summary>
    /// <param name="context"></param>
    public static void SetIsBreaking(this ActivityExecutionContext context) => context.SetProperty<bool>("IsBreaking", true);
}