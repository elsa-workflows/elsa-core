using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class HeartbeatGeneratorTests
{
    [Fact]
    public async Task KeepsBeatingAfterAFailedHeartbeat()
    {
        var calls = 0;
        var secondBeat = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var generator = new HeartbeatGenerator(() =>
        {
            if (Interlocked.Increment(ref calls) == 1)
                throw new InvalidOperationException("Transient failure");

            secondBeat.TrySetResult();
            return Task.CompletedTask;
        }, TimeSpan.FromMilliseconds(20), NullLoggerFactory.Instance);

        await secondBeat.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
