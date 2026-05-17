using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleCaptureTee(
    IConsoleLogProvider provider,
    IConsoleLogSourceRegistry sourceRegistry,
    IConsoleLogRedactor redactor,
    ConsoleLineFormatter formatter,
    IOptions<ConsoleLogsOptions> options) : TextWriter, IConsoleLogCapture
{
    private readonly object _lock = new();
    private readonly ConsoleLogsOptions _options = options.Value;
    private readonly ConsoleLineBuffer _stdoutBuffer = new(options);
    private readonly ConsoleLineBuffer _stderrBuffer = new(options);
    private TextWriter? _originalOut;
    private TextWriter? _originalError;
    private Channel<ConsoleLogLine>? _publishChannel;
    private CancellationTokenSource? _publishCancellation;
    private Task? _publishTask;
    private long _sequence;

    public override Encoding Encoding => _originalOut?.Encoding ?? Encoding.UTF8;

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_originalOut != null || _originalError != null)
                return ValueTask.CompletedTask;

            _publishCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _publishChannel = Channel.CreateBounded<ConsoleLogLine>(new BoundedChannelOptions(Math.Max(1, _options.CaptureChannelCapacity))
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
            _publishTask = PublishQueuedLinesAsync(_publishChannel.Reader, _publishCancellation.Token);
            _originalOut = Console.Out;
            _originalError = Console.Error;
            Console.SetOut(new TeeWriter(_originalOut, this, ConsoleLogStream.Stdout));
            Console.SetError(new TeeWriter(_originalError, this, ConsoleLogStream.Stderr));
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        Task? publishTask;
        CancellationTokenSource? publishCancellation;

        lock (_lock)
        {
            if (_originalOut != null)
                Console.SetOut(_originalOut);

            if (_originalError != null)
                Console.SetError(_originalError);

            FlushRemaining(ConsoleLogStream.Stdout);
            FlushRemaining(ConsoleLogStream.Stderr);
            _publishChannel?.Writer.TryComplete();
            publishTask = _publishTask;
            publishCancellation = _publishCancellation;
            _originalOut = null;
            _originalError = null;
            _publishChannel = null;
            _publishCancellation = null;
            _publishTask = null;
        }

        return AwaitPublisherAsync(publishTask, publishCancellation, cancellationToken);
    }

    public override void Write(char value)
    {
        Capture(ConsoleLogStream.Stdout, value.ToString());
    }

    public override ValueTask DisposeAsync()
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

        if (_publishChannel?.Writer.TryWrite(redactor.Redact(line)) != false)
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
                    await provider.PublishAsync(line, cancellationToken);
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

    private sealed class TeeWriter(TextWriter original, ConsoleCaptureTee capture, ConsoleLogStream stream) : TextWriter
    {
        public override Encoding Encoding => original.Encoding;

        public override void Write(char value)
        {
            original.Write(value);
            capture.Capture(stream, value.ToString());
        }

        public override void Write(string? value)
        {
            original.Write(value);
            if (value != null)
                capture.Capture(stream, value);
        }

        public override void Flush()
        {
            original.Flush();
        }
    }
}
