using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Process-wide adapter that turns raw stdout/stderr writes captured by <see cref="ConsoleStreamHook"/>
/// into fully formed <see cref="ConsoleLogLine"/> records, then publishes them to the configured
/// <see cref="IConsoleLogProvider"/> on a single background task.
///
/// <para>
/// Owned by <see cref="ConsoleLogsHost"/> — there is one instance per process. Capture is wired up in the
/// constructor so that lines emitted during host startup — before any hosted service has run — are not
/// lost. The capture pipeline can never block the caller of <c>Console.Write*</c>: when the publish
/// channel is full, oldest pending lines are evicted and reported as drops to the provider.
/// </para>
///
/// <para>
/// To prevent the publish path itself from feeding new lines back into capture (e.g. when the in-memory
/// provider's SignalR fan-out logs internally), <see cref="ConsoleStreamHook.SuppressCapture"/> is set on
/// the publisher's async context for the lifetime of each <see cref="IConsoleLogProvider.PublishAsync"/>
/// invocation.
/// </para>
/// </summary>
public sealed class ConsoleCaptureTee : IAsyncDisposable, IDisposable
{
    private readonly IConsoleLogProvider _provider;
    private readonly IConsoleLogSourceRegistry _sourceRegistry;
    private readonly IConsoleLogRedactor _redactor;
    private readonly ConsoleLineFormatter _formatter;
    private readonly ConsoleLogScopeAccessor _scopeAccessor;
    private readonly ConsoleLineBuffer _stdoutBuffer;
    private readonly ConsoleLineBuffer _stderrBuffer;
    private readonly object _bufferLock = new();
    private readonly Channel<ConsoleLogLine> _publishChannel;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _publishTask;
    private readonly IDisposable _subscription;
    private long _sequence;
    private int _disposed;

    public ConsoleCaptureTee(
        IConsoleLogProvider provider,
        IConsoleLogSourceRegistry sourceRegistry,
        IConsoleLogRedactor redactor,
        ConsoleLineFormatter formatter,
        ConsoleLogScopeAccessor scopeAccessor,
        IOptions<ConsoleLogsOptions> options)
    {
        _provider = provider;
        _sourceRegistry = sourceRegistry;
        _redactor = redactor;
        _formatter = formatter;
        _scopeAccessor = scopeAccessor;
        _stdoutBuffer = new(options);
        _stderrBuffer = new(options);

        _publishChannel = Channel.CreateBounded<ConsoleLogLine>(new BoundedChannelOptions(Math.Max(1, options.Value.CaptureChannelCapacity))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        _publishTask = Task.Run(() => PublishQueuedLinesAsync(_publishChannel.Reader, _shutdownCts.Token));

        // Subscribe eagerly so that any output produced between this constructor and StartAsync is captured
        // (the hosted service starts the flush loop, not the capture itself).
        _subscription = ConsoleStreamHook.Subscribe(OnChunkCaptured);
    }

    public ValueTask FlushIdleAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        Publish(ConsoleLogStream.Stdout, FlushIfIdle(_stdoutBuffer, now), now);
        Publish(ConsoleLogStream.Stderr, FlushIfIdle(_stderrBuffer, now), now);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _subscription.Dispose();

        // Drain any pending buffered text before completing the channel.
        FlushRemaining(ConsoleLogStream.Stdout);
        FlushRemaining(ConsoleLogStream.Stderr);

        _publishChannel.Writer.TryComplete();

        try
        {
            await _publishTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _shutdownCts.Cancel();
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }

        _shutdownCts.Dispose();
    }

    void IDisposable.Dispose()
    {
        // MS DI requires IDisposable on registered singletons even when the service exposes IAsyncDisposable.
        // Bridge to DisposeAsync so the publish task is drained instead of orphaned.
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private void OnChunkCaptured(CapturedChunk chunk)
    {
        IReadOnlyCollection<BufferedConsoleLine> lines;

        var workflowInstanceId = _scopeAccessor.GetWorkflowInstanceId();

        lock (_bufferLock)
            lines = GetBuffer(chunk.Stream).Append(chunk.Text, chunk.TimestampUtc, workflowInstanceId);

        foreach (var line in lines)
            Publish(chunk.Stream, line, chunk.TimestampUtc);
    }

    private BufferedConsoleLine? FlushIfIdle(ConsoleLineBuffer buffer, DateTimeOffset now)
    {
        lock (_bufferLock)
            return buffer.FlushIfIdle(now);
    }

    private void FlushRemaining(ConsoleLogStream stream)
    {
        BufferedConsoleLine? line;
        lock (_bufferLock)
            line = GetBuffer(stream).Flush();

        Publish(stream, line, DateTimeOffset.UtcNow);
    }

    private ConsoleLineBuffer GetBuffer(ConsoleLogStream stream) =>
        stream == ConsoleLogStream.Stdout ? _stdoutBuffer : _stderrBuffer;

    private void Publish(ConsoleLogStream stream, BufferedConsoleLine? capturedLine, DateTimeOffset timestamp)
    {
        if (capturedLine == null)
            return;

        var text = capturedLine.Value.Text;
        var formatted = _formatter.Format(text);
        var line = new ConsoleLogLine
        {
            Timestamp = timestamp,
            ReceivedAt = timestamp,
            Sequence = Interlocked.Increment(ref _sequence),
            Stream = stream,
            Text = formatted.Text,
            Source = _sourceRegistry.Current,
            WorkflowInstanceId = capturedLine.Value.WorkflowInstanceId,
            Truncated = formatted.Truncated
        };

        // The capture path must not block console writers. When the bounded channel is full, drop this line
        // and surface a summary so the UI can warn the operator.
        if (!_publishChannel.Writer.TryWrite(line) && _provider is IConsoleLogDroppedLineReporter reporter)
            reporter.ReportDropped(new ConsoleLogDroppedSummary(line.Source.Id, stream, "CaptureChannelFull", 1));
    }

    private async Task PublishQueuedLinesAsync(ChannelReader<ConsoleLogLine> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var line in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                // Suppress capture on this async context so that any writes the provider performs
                // (directly or via SignalR fan-out it invokes synchronously) do not loop back into us.
                ConsoleStreamHook.SuppressCapture = true;
                try
                {
                    await _provider.PublishAsync(_redactor.Redact(line), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    // Never log from the publish path — that would route right back through the same tee.
                }
                finally
                {
                    ConsoleStreamHook.SuppressCapture = false;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }
}
