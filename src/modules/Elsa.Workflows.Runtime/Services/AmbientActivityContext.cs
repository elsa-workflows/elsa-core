namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Provides ambient context for activities.
/// </summary>
internal static class AmbientActivityContext
{
    private static readonly AsyncLocal<bool> IsDetachedState = new();

    /// <summary>
    /// Gets or sets a value indicating whether the current activity is detached.
    /// </summary>
    public static bool IsDetached
    {
        get => IsDetachedState.Value;
        set => IsDetachedState.Value = value;
    }
}