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
    private string? _workflowInstanceId;

    public IReadOnlyCollection<BufferedConsoleLine> Append(string value, DateTimeOffset now, string? workflowInstanceId = null)
    {
        var lines = new List<BufferedConsoleLine>();

        foreach (var ch in value)
        {
            if (ch == '\r')
                continue;

            if (ch == '\n')
            {
                if (_buffer.Length > 0)
                    lines.Add(FlushBuffer());
                else if (!_logicalLineHasContent)
                    lines.Add(new(string.Empty, workflowInstanceId));

                _logicalLineHasContent = false;
                continue;
            }

            CaptureScope(workflowInstanceId);
            _buffer.Append(ch);
            _logicalLineHasContent = true;

            if (_buffer.Length >= _options.MaxLineLength)
                lines.Add(FlushBuffer());
        }

        // Track last activity only when characters remain pending so that FlushIfIdle has a deadline to act on.
        _lastWriteAt = _buffer.Length > 0 ? now : null;

        return lines;
    }

    public BufferedConsoleLine? FlushIfIdle(DateTimeOffset now)
    {
        if (_buffer.Length == 0 || _lastWriteAt == null)
            return null;

        return now - _lastWriteAt >= _options.IdleFlushTimeout ? FlushBuffer() : null;
    }

    public BufferedConsoleLine? Flush()
    {
        return _buffer.Length == 0 ? null : FlushBuffer();
    }

    private void CaptureScope(string? workflowInstanceId)
    {
        if (_workflowInstanceId == null && !string.IsNullOrWhiteSpace(workflowInstanceId))
            _workflowInstanceId = workflowInstanceId;
    }

    private BufferedConsoleLine FlushBuffer()
    {
        var line = _buffer.ToString();
        var workflowInstanceId = _workflowInstanceId;
        _buffer.Clear();
        _workflowInstanceId = null;
        return new(line, workflowInstanceId);
    }
}

public readonly record struct BufferedConsoleLine(string Text, string? WorkflowInstanceId);
