using System.Text;
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
    private readonly ConsoleLineBuffer _stdoutBuffer = new(options);
    private readonly ConsoleLineBuffer _stderrBuffer = new(options);
    private TextWriter? _originalOut;
    private TextWriter? _originalError;
    private long _sequence;

    public override Encoding Encoding => _originalOut?.Encoding ?? Encoding.UTF8;

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _originalOut ??= Console.Out;
            _originalError ??= Console.Error;
            Console.SetOut(new TeeWriter(_originalOut, this, ConsoleLogStream.Stdout));
            Console.SetError(new TeeWriter(_originalError, this, ConsoleLogStream.Stderr));
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_originalOut != null)
                Console.SetOut(_originalOut);

            if (_originalError != null)
                Console.SetError(_originalError);

            FlushRemaining(ConsoleLogStream.Stdout);
            FlushRemaining(ConsoleLogStream.Stderr);
            _originalOut = null;
            _originalError = null;
        }

        return ValueTask.CompletedTask;
    }

    public override void Write(char value)
    {
        Capture(ConsoleLogStream.Stdout, value.ToString());
    }

    public override ValueTask DisposeAsync()
    {
        return StopAsync();
    }

    public void FlushIdleLines()
    {
        foreach (var stream in new[] { ConsoleLogStream.Stdout, ConsoleLogStream.Stderr })
        {
            string? line;
            lock (_lock)
                line = GetBuffer(stream).FlushIfIdle(DateTimeOffset.UtcNow);

            if (line != null)
                Publish(stream, line);
        }
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

        provider.PublishAsync(redactor.Redact(line)).AsTask().GetAwaiter().GetResult();
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
