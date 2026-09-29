namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Local UI state for the diagnostics console viewer.
/// </summary>
public class ConsoleLogViewState
{
    private readonly Queue<ConsoleLogLine> _visibleRows = new();
    private readonly Queue<ConsoleLogLine> _pendingRows = new();

    /// <summary>
    /// Gets the current filter.
    /// </summary>
    public ConsoleLogFilter Filter { get; } = new();

    /// <summary>
    /// Gets the bounded local visible rows.
    /// </summary>
    public IReadOnlyCollection<ConsoleLogLine> VisibleRows => _visibleRows;

    /// <summary>
    /// Gets the bounded rows waiting while live rendering is paused.
    /// </summary>
    public IReadOnlyCollection<ConsoleLogLine> PendingRows => _pendingRows;

    /// <summary>
    /// Gets or sets the maximum number of local visible rows.
    /// </summary>
    public int VisibleRowCap { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets the count of rows discarded because of the local cap.
    /// </summary>
    public long DiscardedLocalRows { get; set; }

    /// <summary>
    /// Gets or sets the current connection status.
    /// </summary>
    public ConsoleLogConnectionStatus ConnectionStatus { get; set; } = ConsoleLogConnectionStatus.Disconnected;

    /// <summary>
    /// Gets or sets a value indicating whether live rendering is paused.
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets the number of lines waiting while paused.
    /// </summary>
    public long PendingLineCount { get; set; }

    /// <summary>
    /// Gets or sets the count of paused rows discarded because of the local pending cap.
    /// </summary>
    public long DiscardedPendingRows { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the viewer follows the latest line.
    /// </summary>
    public bool FollowTail { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether lines wrap.
    /// </summary>
    public bool Wrap { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether compact density is enabled.
    /// </summary>
    public bool Compact { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether raw ANSI sequences are preserved in the viewer.
    /// </summary>
    public bool Ansi { get; set; } = true;

    /// <summary>
    /// Adds a visible line and prunes older rows according to the local cap.
    /// </summary>
    public void AddVisibleLine(ConsoleLogLine line)
    {
        _visibleRows.Enqueue(line);

        while (_visibleRows.Count > VisibleRowCap)
        {
            _visibleRows.Dequeue();
            DiscardedLocalRows++;
        }
    }

    /// <summary>
    /// Adds an incoming live line to the visible buffer or the paused pending buffer.
    /// </summary>
    public void AddIncomingLine(ConsoleLogLine line)
    {
        if (IsPaused)
        {
            AddPendingLine(line);
            return;
        }

        AddVisibleLine(line);
    }

    /// <summary>
    /// Moves pending rows into the visible buffer and clears the pending buffer.
    /// </summary>
    public void FlushPendingRows()
    {
        while (_pendingRows.TryDequeue(out var line))
            AddVisibleLine(line);

        PendingLineCount = 0;
    }

    /// <summary>
    /// Clears local visible rows without changing backend capture.
    /// </summary>
    public void ClearVisibleRows()
    {
        _visibleRows.Clear();
        _pendingRows.Clear();
        PendingLineCount = 0;
        DiscardedLocalRows = 0;
        DiscardedPendingRows = 0;
    }

    private void AddPendingLine(ConsoleLogLine line)
    {
        _pendingRows.Enqueue(line);

        while (_pendingRows.Count > VisibleRowCap)
        {
            _pendingRows.Dequeue();
            DiscardedPendingRows++;
        }

        PendingLineCount = _pendingRows.Count;
    }
}
