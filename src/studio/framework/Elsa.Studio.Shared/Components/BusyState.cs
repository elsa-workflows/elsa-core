namespace Elsa.Studio.Components;

/// <summary>
/// Tracks the operation behind a busy button: repeat clicks are ignored while it runs, and an operation that navigates
/// away leaves the control busy, so it does not flash back to idle before the page unloads.
/// </summary>
public sealed class BusyState
{
    /// <summary>Whether the operation is in flight, or completed by navigating away.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Runs <paramref name="operation"/> unless one is already in flight.</summary>
    /// <param name="operation">Returns <c>true</c> when it navigated away from the page.</param>
    public async Task RunAsync(Func<Task<bool>> operation)
    {
        if (IsBusy)
            return;

        // Go busy before the first await so the calling event handler renders the busy state while the operation runs.
        IsBusy = true;
        var navigatedAway = false;

        try
        {
            navigatedAway = await operation();
        }
        finally
        {
            IsBusy = navigatedAway;
        }
    }
}
