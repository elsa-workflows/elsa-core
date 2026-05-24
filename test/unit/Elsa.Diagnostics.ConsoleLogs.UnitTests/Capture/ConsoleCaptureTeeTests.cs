using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Capture;

[Collection(ConsoleHostStateCollection.Name)]
public class ConsoleCaptureTeeTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextWriter _originalError = Console.Error;
    private readonly StringWriter _consoleOutput = new();
    private readonly CapturingProvider _provider = new();
    private readonly IOptions<ConsoleLogsOptions> _options = Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions());

    public ConsoleCaptureTeeTests()
    {
        // Tear down any process-wide host instance so its hook subscription doesn't double-publish.
        ConsoleLogsHost.ShutdownAsync().AsTask().GetAwaiter().GetResult();
        ConsoleStreamHook.Uninstall();
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        ConsoleStreamHook.Uninstall();
        ConsoleLogsHost.ShutdownAsync().AsTask().GetAwaiter().GetResult();
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        _consoleOutput.Dispose();
    }

    private ConsoleCaptureTee CreateCapture() => new(
        _provider,
        new ConsoleLogSourceRegistry(_options),
        new ConsoleLogRedactor(_options),
        new ConsoleLineFormatter(_options),
        _options);

    [Fact]
    public async Task Capture_PreservesOriginalConsoleOutputAndPublishesLine()
    {
        await using var capture = CreateCapture();

        Console.WriteLine("hello");
        await WaitForLinesAsync(_provider, 1);

        Assert.Equal($"hello{Environment.NewLine}", _consoleOutput.ToString());
        var line = Assert.Single(_provider.Lines);
        Assert.Equal("hello", line.Text);
        Assert.Equal(ConsoleLogStream.Stdout, line.Stream);
    }

    [Fact]
    public async Task Capture_RedactsBeforePublishingToProvider()
    {
        await using var capture = CreateCapture();

        Console.WriteLine("token=secret-value");
        await WaitForLinesAsync(_provider, 1);

        Assert.Equal("[Redacted]", _provider.Lines[0].Text);
    }

    [Fact]
    public async Task Capture_AfterDispose_StopsPublishing()
    {
        var capture = CreateCapture();

        Console.WriteLine("first");
        await WaitForLinesAsync(_provider, 1);

        await capture.DisposeAsync();

        Console.WriteLine("second");
        await Task.Delay(50);

        Assert.Single(_provider.Lines);
        Assert.Equal("first", _provider.Lines[0].Text);
    }

    [Fact]
    public async Task Capture_FansOutToMultipleConcurrentSubscribers()
    {
        var secondProvider = new CapturingProvider();
        await using var first = CreateCapture();
        await using var second = new ConsoleCaptureTee(
            secondProvider,
            new ConsoleLogSourceRegistry(_options),
            new ConsoleLogRedactor(_options),
            new ConsoleLineFormatter(_options),
            _options);

        Console.WriteLine("broadcast");
        await WaitForLinesAsync(_provider, 1);
        await WaitForLinesAsync(secondProvider, 1);

        Assert.Equal("broadcast", _provider.Lines[^1].Text);
        Assert.Equal("broadcast", secondProvider.Lines[^1].Text);
    }

    [Fact]
    public async Task Capture_SuppressesPublishPathReentrancy()
    {
        var loggingProvider = new ReentrantProvider();
        await using var capture = new ConsoleCaptureTee(
            loggingProvider,
            new ConsoleLogSourceRegistry(_options),
            new ConsoleLogRedactor(_options),
            new ConsoleLineFormatter(_options),
            _options);

        Console.WriteLine("trigger");
        await WaitForLinesAsync(loggingProvider, 1);
        await Task.Delay(50); // Give any reentrant publish a chance to misbehave.

        Assert.Single(loggingProvider.Lines);
        Assert.Equal("trigger", loggingProvider.Lines[0].Text);
    }

    private static async Task WaitForLinesAsync(CapturingProvider provider, int count)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (provider.Lines.Count < count && DateTimeOffset.UtcNow < deadline)
            await Task.Delay(10);
    }

    private class CapturingProvider : IConsoleLogProvider
    {
        public List<ConsoleLogLine> Lines { get; } = [];

        public virtual ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            lock (Lines)
                Lines.Add(line);

            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new RecentConsoleLogsResult(Lines));

        public async IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            foreach (var line in Lines)
                yield return ConsoleLogStreamItem.FromLine(line);
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
    }

    private sealed class ReentrantProvider : CapturingProvider
    {
        public override ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            // Simulate a provider whose internals write to the console (e.g., SignalR diagnostics).
            // With SuppressCapture set on this async context, this MUST NOT recurse.
            Console.WriteLine("[provider-internal-log]");
            return base.PublishAsync(line, cancellationToken);
        }
    }
}
