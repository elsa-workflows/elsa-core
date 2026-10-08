namespace Elsa.Workflows;

/// <summary>
/// Provides extension methods for <see cref="ActivityExecutionContext"/>. 
/// </summary>
public static class BreakBehaviorActivityExecutionContextExtensions
{
    /// <param name="context"></param>
    extension(ActivityExecutionContext context)
    {
        /// <summary>
        /// Gets a value indicating whether the current activity is breaking out of a loop.
        /// </summary>
        public bool GetIsBreaking() => context.GetProperty<bool>("IsBreaking");

        /// <summary>
        /// Sets a value indicating whether the current activity is breaking out of a loop.
        /// </summary>
        public void SetIsBreaking() => context.SetProperty<bool>("IsBreaking", true);
    }
}