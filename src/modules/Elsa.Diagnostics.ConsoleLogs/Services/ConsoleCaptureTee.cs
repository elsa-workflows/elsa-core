using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleCaptureTee(
    IConsoleLogProvider provider,
    IConsoleLogSourceRegistry sourceRegistry,
    IConsoleLogRedactor redactor,
    ConsoleLineFormatter formatter,
    IOptions<ConsoleLogsOptions> options) : IConsoleLogCapture, IDisposable
{
    private readonly object _lock = new();
    private readonly ConsoleLogsOptions _options = options.Value;
    private readonly ConsoleLineBuffer _stdoutBuffer = new(options);
    private readonly ConsoleLineBuffer _stderrBuffer = new(options);
    private Channel<ConsoleLogLine>? _publishChannel;
    private CancellationTokenSource? _publishCancellation;
    private Task? _publishTask;
    private int _startCount;
    private long _sequence;
    private Action<ConsoleLogStream, string>? _hookHandler;

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_startCount++ > 0)
                return ValueTask.CompletedTask;

            _publishCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _publishChannel = Channel.CreateBounded<ConsoleLogLine>(new BoundedChannelOptions(Math.Max(1, _options.CaptureChannelCapacity))
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
            _publishTask = PublishQueuedLinesAsync(_publishChannel.Reader, _publishCancellation.Token);

            // Ensure the tee is installed (idempotent), then subscribe this capture instance.
            ConsoleStreamHook.Install();
            _hookHandler = Capture;
            ConsoleStreamHook.OnCapture = _hookHandler;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        Task? publishTask;
        CancellationTokenSource? publishCancellation;

        lock (_lock)
        {
            if (_startCount == 0)
                return ValueTask.CompletedTask;

            if (--_startCount > 0)
                return ValueTask.CompletedTask;

            if (ReferenceEquals(ConsoleStreamHook.OnCapture, _hookHandler))
                ConsoleStreamHook.OnCapture = null;
            _hookHandler = null;

            FlushRemaining(ConsoleLogStream.Stdout);
            FlushRemaining(ConsoleLogStream.Stderr);
            _publishChannel?.Writer.TryComplete();
            publishTask = _publishTask;
            publishCancellation = _publishCancellation;
            _publishChannel = null;
            _publishCancellation = null;
            _publishTask = null;
        }

        return AwaitPublisherAsync(publishTask, publishCancellation, cancellationToken);
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    public ValueTask DisposeAsync()
    {
        return StopAsync();
    }

    public ValueTask FlushIdleAsync(CancellationToken cancellationToken = default)
    {
        foreach (var stream in new[] { ConsoleLogStream.Stdout, ConsoleLogStream.Stderr })
        {
            string? line;
            lock (_lock)
                line = GetBuffer(stream).FlushIfIdle(DateTimeOffset.UtcNow);

            if (line != null)
                Publish(stream, line);
        }

        return ValueTask.CompletedTask;
    }

    private void Capture(ConsoleLogStream stream, string value)
    {
        IReadOnlyCollection<string> lines;
        lock (_lock)
            lines = GetBuffer(stream).Append(value, DateTimeOffset.UtcNow);

        foreach (var line in lines)
            Publish(stream, line);
    }

    private void FlushRemaining(ConsoleLogStream stream)
    {
        var line = GetBuffer(stream).Flush();
        if (line != null)
            Publish(stream, line);
    }

    private ConsoleLineBuffer GetBuffer(ConsoleLogStream stream) => stream == ConsoleLogStream.Stdout ? _stdoutBuffer : _stderrBuffer;

    private void Publish(ConsoleLogStream stream, string text)
    {
        var formatted = formatter.Format(text);
        var now = DateTimeOffset.UtcNow;
        var line = new ConsoleLogLine
        {
            Timestamp = now,
            ReceivedAt = now,
            Sequence = Interlocked.Increment(ref _sequence),
            Stream = stream,
            Text = formatted.Text,
            Source = sourceRegistry.Current,
            Truncated = formatted.Truncated
        };

        if (_publishChannel?.Writer.TryWrite(line) != false)
            return;

        if (provider is IConsoleLogDroppedLineReporter reporter)
            reporter.ReportDropped(new ConsoleLogDroppedSummary(line.Source.Id, stream, "CaptureChannelFull", 1));
    }

    private async Task PublishQueuedLinesAsync(ChannelReader<ConsoleLogLine> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var line in reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await provider.PublishAsync(redactor.Redact(line), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    // Avoid logging from the console capture path; doing so would recurse through the same tee.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown completes or cancels the publish pump.
        }
    }

    private static async ValueTask AwaitPublisherAsync(Task? publishTask, CancellationTokenSource? publishCancellation, CancellationToken cancellationToken)
    {
        if (publishTask == null)
            return;

        using var cancellation = publishCancellation;

        try
        {
            await publishTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellation?.Cancel();
            throw;
        }
    }
}

