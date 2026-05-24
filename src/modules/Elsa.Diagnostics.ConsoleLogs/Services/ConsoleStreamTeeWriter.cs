using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// A <see cref="TextWriter"/> that forwards every write to the original underlying writer and a configurable capture sink.
/// </summary>
internal sealed class ConsoleStreamTeeWriter(TextWriter original, ConsoleLogStream stream, Action<ConsoleLogStream, string> capture) : TextWriter
{
    public TextWriter Original => original;

    public override Encoding Encoding => original.Encoding;
    public override IFormatProvider FormatProvider => original.FormatProvider;
    [AllowNull]
    public override string NewLine { get => original.NewLine; set => original.NewLine = value; }

    // ── Write overloads ──

    public override void Write(char value)
    {
        original.Write(value);
        capture(stream, value.ToString());
    }

    public override void Write(char[]? buffer)
    {
        original.Write(buffer);
        if (buffer is { Length: > 0 })
            capture(stream, new string(buffer));
    }

    public override void Write(char[] buffer, int index, int count)
    {
        original.Write(buffer, index, count);
        if (count > 0)
            capture(stream, new string(buffer, index, count));
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        original.Write(buffer);
        if (!buffer.IsEmpty)
            capture(stream, buffer.ToString());
    }

    public override void Write(bool value) { original.Write(value); capture(stream, value ? "True" : "False"); }
    public override void Write(int value) { original.Write(value); capture(stream, value.ToString(FormatProvider)); }
    public override void Write(uint value) { original.Write(value); capture(stream, value.ToString(FormatProvider)); }
    public override void Write(long value) { original.Write(value); capture(stream, value.ToString(FormatProvider)); }
    public override void Write(ulong value) { original.Write(value); capture(stream, value.ToString(FormatProvider)); }
    public override void Write(float value) { original.Write(value); capture(stream, value.ToString(FormatProvider)); }
    public override void Write(double value) { original.Write(value); capture(stream, value.ToString(FormatProvider)); }
    public override void Write(decimal value) { original.Write(value); capture(stream, value.ToString(FormatProvider)); }

    public override void Write(string? value)
    {
        original.Write(value);
        if (value != null)
            capture(stream, value);
    }

    public override void Write(object? value)
    {
        original.Write(value);
        var text = value?.ToString();
        if (text != null)
            capture(stream, text);
    }

    public override void Write(StringBuilder? value)
    {
        original.Write(value);
        if (value is { Length: > 0 })
            capture(stream, value.ToString());
    }

    public override void Write(string format, object? arg0) { var text = string.Format(FormatProvider, format, arg0); original.Write(text); capture(stream, text); }
    public override void Write(string format, object? arg0, object? arg1) { var text = string.Format(FormatProvider, format, arg0, arg1); original.Write(text); capture(stream, text); }
    public override void Write(string format, object? arg0, object? arg1, object? arg2) { var text = string.Format(FormatProvider, format, arg0, arg1, arg2); original.Write(text); capture(stream, text); }
    public override void Write(string format, params object?[] arg) { var text = string.Format(FormatProvider, format, arg); original.Write(text); capture(stream, text); }
#if NET9_0_OR_GREATER
    public override void Write(string format, params ReadOnlySpan<object?> arg) { var text = string.Format(FormatProvider, format, arg); original.Write(text); capture(stream, text); }
#endif

    // ── WriteLine overloads ──

    public override void WriteLine() { original.WriteLine(); capture(stream, "\n"); }
    public override void WriteLine(char value) { original.WriteLine(value); capture(stream, value + "\n"); }

    public override void WriteLine(char[]? buffer)
    {
        original.WriteLine(buffer);
        capture(stream, (buffer is { Length: > 0 } ? new string(buffer) : "") + "\n");
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        original.WriteLine(buffer, index, count);
        capture(stream, new string(buffer, index, count) + "\n");
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        original.WriteLine(buffer);
        capture(stream, buffer.ToString() + "\n");
    }

    public override void WriteLine(bool value) { original.WriteLine(value); capture(stream, (value ? "True" : "False") + "\n"); }
    public override void WriteLine(int value) { original.WriteLine(value); capture(stream, value.ToString(FormatProvider) + "\n"); }
    public override void WriteLine(uint value) { original.WriteLine(value); capture(stream, value.ToString(FormatProvider) + "\n"); }
    public override void WriteLine(long value) { original.WriteLine(value); capture(stream, value.ToString(FormatProvider) + "\n"); }
    public override void WriteLine(ulong value) { original.WriteLine(value); capture(stream, value.ToString(FormatProvider) + "\n"); }
    public override void WriteLine(float value) { original.WriteLine(value); capture(stream, value.ToString(FormatProvider) + "\n"); }
    public override void WriteLine(double value) { original.WriteLine(value); capture(stream, value.ToString(FormatProvider) + "\n"); }
    public override void WriteLine(decimal value) { original.WriteLine(value); capture(stream, value.ToString(FormatProvider) + "\n"); }

    public override void WriteLine(string? value)
    {
        original.WriteLine(value);
        capture(stream, (value ?? "") + "\n");
    }

    public override void WriteLine(object? value)
    {
        original.WriteLine(value);
        capture(stream, (value?.ToString() ?? "") + "\n");
    }

    public override void WriteLine(StringBuilder? value)
    {
        original.WriteLine(value);
        capture(stream, (value is { Length: > 0 } ? value.ToString() : "") + "\n");
    }

    public override void WriteLine(string format, object? arg0) { var text = string.Format(FormatProvider, format, arg0); original.WriteLine(text); capture(stream, text + "\n"); }
    public override void WriteLine(string format, object? arg0, object? arg1) { var text = string.Format(FormatProvider, format, arg0, arg1); original.WriteLine(text); capture(stream, text + "\n"); }
    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2) { var text = string.Format(FormatProvider, format, arg0, arg1, arg2); original.WriteLine(text); capture(stream, text + "\n"); }
    public override void WriteLine(string format, params object?[] arg) { var text = string.Format(FormatProvider, format, arg); original.WriteLine(text); capture(stream, text + "\n"); }
#if NET9_0_OR_GREATER
    public override void WriteLine(string format, params ReadOnlySpan<object?> arg) { var text = string.Format(FormatProvider, format, arg); original.WriteLine(text); capture(stream, text + "\n"); }
#endif

    // ── Async Write overloads ──

    public override async Task WriteAsync(char value) { await original.WriteAsync(value); capture(stream, value.ToString()); }

    public override async Task WriteAsync(string? value)
    {
        await original.WriteAsync(value);
        if (value != null)
            capture(stream, value);
    }

    public override async Task WriteAsync(char[] buffer, int index, int count)
    {
        await original.WriteAsync(buffer, index, count);
        if (count > 0)
            capture(stream, new string(buffer, index, count));
    }

    public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
    {
        await original.WriteAsync(buffer, cancellationToken);
        if (!buffer.IsEmpty)
            capture(stream, buffer.ToString());
    }

    public override async Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = default)
    {
        await original.WriteAsync(value, cancellationToken);
        if (value is { Length: > 0 })
            capture(stream, value.ToString());
    }

    // ── Async WriteLine overloads ──

    public override async Task WriteLineAsync() { await original.WriteLineAsync(); capture(stream, "\n"); }
    public override async Task WriteLineAsync(char value) { await original.WriteLineAsync(value); capture(stream, value + "\n"); }

    public override async Task WriteLineAsync(string? value)
    {
        await original.WriteLineAsync(value);
        capture(stream, (value ?? "") + "\n");
    }

    public override async Task WriteLineAsync(char[] buffer, int index, int count)
    {
        await original.WriteLineAsync(buffer, index, count);
        capture(stream, new string(buffer, index, count) + "\n");
    }

    public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
    {
        await original.WriteLineAsync(buffer, cancellationToken);
        capture(stream, buffer.ToString() + "\n");
    }

    public override async Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = default)
    {
        await original.WriteLineAsync(value, cancellationToken);
        capture(stream, (value is { Length: > 0 } ? value.ToString() : "") + "\n");
    }

    // ── Lifecycle ──

    public override void Flush() => original.Flush();
    public override Task FlushAsync() => original.FlushAsync();
    public override Task FlushAsync(CancellationToken cancellationToken) => original.FlushAsync(cancellationToken);
    public override void Close() => original.Close();
}

