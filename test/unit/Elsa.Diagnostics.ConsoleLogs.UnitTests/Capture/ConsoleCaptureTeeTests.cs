using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Capture;

[Collection(ConsoleHostStateCollection.Name)]
public class ConsoleCaptureTeeTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextWriter _originalError = Console.Error;
    private readonly StringWriter _consoleOutput = new();
    private readonly CapturingProvider _provider = new();
    private readonly IOptions<ConsoleLogsOptions> _options = Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions());
    private readonly ConsoleLogScopeAccessor _scopeAccessor = new();

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
        _scopeAccessor,
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
            _scopeAccessor,
            _options);

        Console.WriteLine("broadcast");
        await WaitForLinesAsync(_provider, 1);
        await WaitForLinesAsync(secondProvider, 1);

        Assert.Equal("broadcast", _provider.Lines[^1].Text);
        Assert.Equal("broadcast", secondProvider.Lines[^1].Text);
    }

    [Fact]
    public async Task Capture_ReplaysStartupWritesOnlyOnce()
    {
        ConsoleStreamHook.Install();
        Console.WriteLine("startup");

        var secondProvider = new CapturingProvider();
        await using var first = CreateCapture();
        await WaitForLinesAsync(_provider, 1);

        await using var second = new ConsoleCaptureTee(
            secondProvider,
            new ConsoleLogSourceRegistry(_options),
            new ConsoleLogRedactor(_options),
            new ConsoleLineFormatter(_options),
            _scopeAccessor,
            _options);
        await Task.Delay(50);

        Assert.Equal("startup", _provider.Lines[0].Text);
        Assert.Empty(secondProvider.Lines);
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
            _scopeAccessor,
            _options);

        Console.WriteLine("trigger");
        await WaitForLinesAsync(loggingProvider, 1);
        await Task.Delay(50); // Give any reentrant publish a chance to misbehave.

        Assert.Single(loggingProvider.Lines);
        Assert.Equal("trigger", loggingProvider.Lines[0].Text);
    }

    [Fact]
    public async Task Capture_ContinuesAfterProviderOperationCanceledException()
    {
        var provider = new OperationCanceledOnceProvider();
        await using var capture = new ConsoleCaptureTee(
            provider,
            new ConsoleLogSourceRegistry(_options),
            new ConsoleLogRedactor(_options),
            new ConsoleLineFormatter(_options),
            _scopeAccessor,
            _options);

        Console.WriteLine("first");
        Console.WriteLine("second");
        await WaitForLinesAsync(provider, 1);

        var line = Assert.Single(provider.Lines);
        Assert.Equal("second", line.Text);
    }

    [Fact]
    public async Task Capture_AttachesCurrentWorkflowInstanceId()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_scopeAccessor));
        var logger = loggerFactory.CreateLogger("workflow");
        await using var capture = CreateCapture();
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["WorkflowInstanceId"] = "workflow-instance-a"
        });

        Console.WriteLine("scoped");
        await WaitForLinesAsync(_provider, 1);

        Assert.Equal("workflow-instance-a", _provider.Lines[0].WorkflowInstanceId);
    }

    [Fact]
    public async Task Capture_AttachesInnermostWorkflowInstanceId()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_scopeAccessor));
        var logger = loggerFactory.CreateLogger("workflow");
        await using var capture = CreateCapture();
        using var outerScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["WorkflowInstanceId"] = "workflow-instance-a"
        });
        using var innerScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["WorkflowInstanceId"] = "workflow-instance-b"
        });

        Console.WriteLine("scoped");
        await WaitForLinesAsync(_provider, 1);

        Assert.Equal("workflow-instance-b", _provider.Lines[0].WorkflowInstanceId);
    }

    [Fact]
    public async Task Capture_PreservesScopeAcrossMultipleLoggerFactories()
    {
        using var firstLoggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_scopeAccessor));
        using var secondLoggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_scopeAccessor));
        var logger = firstLoggerFactory.CreateLogger("workflow");
        await using var capture = CreateCapture();
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["WorkflowInstanceId"] = "workflow-instance-a"
        });

        _ = secondLoggerFactory.CreateLogger("other");
        Console.WriteLine("scoped");
        await WaitForLinesAsync(_provider, 1);

        Assert.Equal("workflow-instance-a", _provider.Lines[0].WorkflowInstanceId);
    }

    [Fact]
    public async Task Capture_AttachesWorkflowInstanceIdCapturedBeforeAsyncConsoleWrite()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_scopeAccessor));
        var logger = loggerFactory.CreateLogger("workflow");
        await using var capture = CreateCapture();

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["WorkflowInstanceId"] = "workflow-instance-a"
        }))
        {
            logger.LogInformation("scoped");
        }

        await Task.Run(() => Console.WriteLine("scoped"));
        await WaitForLinesAsync(_provider, 1);

        Assert.Equal("workflow-instance-a", _provider.Lines[0].WorkflowInstanceId);
    }

    [Fact]
    public async Task Capture_DoesNotAttachCapturedWorkflowInstanceIdToUnrelatedConsoleWrite()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_scopeAccessor));
        var logger = loggerFactory.CreateLogger("workflow");
        await using var capture = CreateCapture();

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["WorkflowInstanceId"] = "workflow-instance-a"
        }))
        {
            logger.LogInformation("scoped");
        }

        await Task.Run(() => Console.WriteLine("unrelated"));
        await Task.Run(() => Console.WriteLine("scoped"));
        await WaitForLinesAsync(_provider, 2);

        Assert.Null(_provider.Lines[0].WorkflowInstanceId);
        Assert.Equal("workflow-instance-a", _provider.Lines[1].WorkflowInstanceId);
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

    private sealed class OperationCanceledOnceProvider : CapturingProvider
    {
        private int _throwCount;

        public override ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _throwCount) == 1)
                throw new OperationCanceledException();

            return base.PublishAsync(line, cancellationToken);
        }
    }
}
