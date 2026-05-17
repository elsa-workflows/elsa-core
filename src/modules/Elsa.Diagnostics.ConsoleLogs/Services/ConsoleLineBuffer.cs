using System.Text;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleLineBuffer(IOptions<ConsoleLogsOptions> options)
{
    private readonly StringBuilder _buffer = new();
    private readonly ConsoleLogsOptions _options = options.Value;
    private DateTimeOffset? _lastWriteAt;

    public IReadOnlyCollection<string> Append(string value, DateTimeOffset now)
    {
        _lastWriteAt = now;
        var lines = new List<string>();

        foreach (var ch in value)
        {
            if (ch == '\r')
                continue;

            if (ch == '\n')
            {
                if (_buffer.Length > 0)
                    lines.Add(FlushBuffer());

                continue;
            }

            _buffer.Append(ch);

            if (_buffer.Length >= _options.MaxLineLength)
                lines.Add(FlushBuffer());
        }

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
        _lastWriteAt = null;
        return line;
    }
}
