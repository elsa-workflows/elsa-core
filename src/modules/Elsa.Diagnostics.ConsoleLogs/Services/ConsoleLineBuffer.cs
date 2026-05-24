using System.Text;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Reassembles arbitrary stdout/stderr write chunks into complete lines. Lines are emitted whenever a
/// terminator (<c>\n</c>) is encountered, when the buffer reaches <see cref="ConsoleLogsOptions.MaxLineLength"/>,
/// or when <see cref="FlushIfIdle"/> is called after the configured idle timeout has elapsed.
/// Empty lines (a bare <c>\n</c> with no preceding content on the current logical line) are preserved.
/// </summary>
public class ConsoleLineBuffer(IOptions<ConsoleLogsOptions> options)
{
    private readonly StringBuilder _buffer = new();
    private readonly ConsoleLogsOptions _options = options.Value;
    private DateTimeOffset? _lastWriteAt;
    private bool _logicalLineHasContent;

    public IReadOnlyCollection<string> Append(string value, DateTimeOffset now)
    {
        var lines = new List<string>();

        foreach (var ch in value)
        {
            if (ch == '\r')
                continue;

            if (ch == '\n')
            {
                if (_buffer.Length > 0)
                    lines.Add(FlushBuffer());
                else if (!_logicalLineHasContent)
                    lines.Add(string.Empty);

                _logicalLineHasContent = false;
                continue;
            }

            _buffer.Append(ch);
            _logicalLineHasContent = true;

            if (_buffer.Length >= _options.MaxLineLength)
                lines.Add(FlushBuffer());
        }

        // Track last activity only when characters remain pending so that FlushIfIdle has a deadline to act on.
        _lastWriteAt = _buffer.Length > 0 ? now : null;

        return lines;
    }

    public string? FlushIfIdle(DateTimeOffset now)
    {
        if (_buffer.Length == 0 || _lastWriteAt == null)
            return null;

        return now - _lastWriteAt >= _options.IdleFlushTimeout ? FlushBuffer() : null;
    }

    public string? Flush()
    {
        return _buffer.Length == 0 ? null : FlushBuffer();
    }

    private string FlushBuffer()
    {
        var line = _buffer.ToString();
        _buffer.Clear();
        return line;
    }
}
