using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class HeartbeatGeneratorTests
{
    [Fact]
    public async Task KeepsBeating_AfterTransientFailure()
    {
        var calls = 0;
        var secondCall = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var generator = new HeartbeatGenerator(() =>
        {
            if (Interlocked.Increment(ref calls) == 1)
                throw new InvalidOperationException("Transient failure");

            secondCall.TrySetResult();
            return Task.CompletedTask;
        }, TimeSpan.FromMilliseconds(20), NullLoggerFactory.Instance);

        var completed = await Task.WhenAny(secondCall.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.Same(secondCall.Task, completed);
    }
}
