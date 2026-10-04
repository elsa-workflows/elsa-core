namespace Elsa.Workflows.Runtime.UnitTests.Support;

/// <summary>
/// Coordinates a test thread with a callback that must block until the test releases it.
/// Timeouts are recorded so <see cref="AssertNotTimedOut"/> can fail the test even when
/// the callback exception is swallowed (for example by <c>ExecutionCycleHandle.TryCancel</c>).
/// </summary>
internal sealed class CallbackGate(string name)
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string Name { get; } = name;

    public bool TimedOut { get; private set; }

    public Task Entered => _entered.Task;

    public void SignalEntered() => _entered.TrySetResult();

    public void WaitForRelease()
    {
        if (_release.Task.Wait(Timeout))
            return;

        TimedOut = true;
        throw new TimeoutException($"Timed out waiting for the test to release {Name}.");
    }

    public void Release() => _release.TrySetResult();

    public void AssertNotTimedOut() =>
        Assert.False(TimedOut, $"Callback gate '{Name}' timed out waiting for release.");

    public static async Task ObserveCleanupAsync(Task? task)
    {
        if (task is null)
            return;

        try
        {
            await task.WaitAsync(Timeout);
        }
        catch (TimeoutException)
        {
            // Preserve the original assertion/timeout while observing the cleanup task.
        }
        catch (OperationCanceledException)
        {
            // Preserve the original assertion/timeout while observing the cleanup task.
        }
    }
}
