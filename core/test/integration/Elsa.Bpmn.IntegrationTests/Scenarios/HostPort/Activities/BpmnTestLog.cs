using Elsa.Bpmn.Hosting;
using Elsa.Workflows;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

/// <summary>
/// An ordered record of what the stand-in work activities did, plus snapshots taken at moments the applier's
/// invariants are observable.
/// </summary>
public sealed class BpmnTestLog
{
    private readonly List<string> _entries = [];
    private readonly Dictionary<string, IReadOnlyList<string>> _snapshots = new(StringComparer.Ordinal);

    /// <summary>Everything that happened, in order.</summary>
    public IReadOnlyList<string> Entries => _entries;

    /// <summary>Records that something happened.</summary>
    public void Record(string entry) => _entries.Add(entry);

    /// <summary>The position of an entry, or <c>-1</c>.</summary>
    public int PositionOf(string entry) => _entries.IndexOf(entry);

    /// <summary>How many times an entry was recorded.</summary>
    public int Occurrences(string entry) => _entries.Count(x => string.Equals(x, entry, StringComparison.Ordinal));

    /// <summary>A snapshot taken under the given key.</summary>
    public IReadOnlyList<string> Snapshot(string key) =>
        _snapshots.TryGetValue(key, out var snapshot)
            ? snapshot
            : throw new InvalidOperationException($"Nothing was captured under '{key}'. Captured keys: {string.Join(", ", _snapshots.Keys)}.");

    /// <summary>
    /// Captures the binding refs the enclosing BPMN scope currently reports as live work, read from the scope's own
    /// persisted ledger — which is what the host hands the interpreter as <c>BpmnHostSnapshot.LiveWork</c>.
    /// </summary>
    public void CaptureLiveWork(string key, ActivityExecutionContext context)
    {
        var scopeContext = context.ParentActivityExecutionContext
                           ?? throw new InvalidOperationException("Expected the work activity to be owned by a BPMN scope.");
        var ledger = BpmnScopeMemory.Read<BpmnWorkLedger>(scopeContext, BpmnScopeMemory.WorkLedgerPropertyKey) ?? new BpmnWorkLedger();

        _snapshots[key] = ledger.Records.Select(x => x.BindingRef).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    /// <summary>Captures the ids of the activities currently sitting in the workflow's scheduler.</summary>
    public void CaptureScheduled(string key, ActivityExecutionContext context) =>
        _snapshots[key] = context.WorkflowExecutionContext.Scheduler.List().Select(x => x.Activity.Id).ToList();
}
