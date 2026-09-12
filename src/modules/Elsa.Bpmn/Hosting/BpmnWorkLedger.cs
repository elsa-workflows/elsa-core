using Bpmn.Semantics;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// A BPMN scope's record of the work it has started and not finished — the handle-to-context map, and the source of
/// <see cref="BpmnHostSnapshot.LiveWork"/>.
/// </summary>
/// <remarks>
/// <para>
/// What belongs in here depends on which callback the scope is about to make, and the difference is load-bearing:
/// completing and faulting work must already have been removed, because leaving it lets a re-armed non-interrupting
/// listener key onto the same <c>(binding ref, iteration id)</c> slot and a teardown then targets the work that just
/// finished; signalling work must still be present, because an escalating activity keeps running and removing it
/// makes the interpreter believe it has already gone.
/// </para>
/// <para>
/// The interpreter re-finds a parked token from <c>(binding ref, iteration id)</c> alone, so a scope must never hold
/// two live units of work under one such pair. Multi-instance instances share a binding ref and are told apart by
/// their iteration id, which is what it is for.
/// </para>
/// </remarks>
internal sealed class BpmnWorkLedger
{
    /// <summary>The handle counter. Handles are scope-local so a scope's trace reads independently of Elsa's id generator.</summary>
    public int Sequence { get; set; }

    /// <summary>The live work, in start order.</summary>
    public List<BpmnWorkRecord> Records { get; set; } = [];

    /// <summary>Mints the next scope-local handle.</summary>
    public string NextHandle() => $"work-{++Sequence}";

    /// <summary>The live work running in the given child activity execution, or <c>null</c>.</summary>
    public BpmnWorkRecord? FindByChildContextId(string childContextId) =>
        Records.FirstOrDefault(x => string.Equals(x.ChildContextId, childContextId, StringComparison.Ordinal));

    /// <summary>The live work with the given handle, or <c>null</c>.</summary>
    public BpmnWorkRecord? FindByHandle(string handle) =>
        Records.FirstOrDefault(x => string.Equals(x.Handle, handle, StringComparison.Ordinal));

    /// <summary>Drops a record, so the work is no longer reported as live.</summary>
    public void Remove(BpmnWorkRecord record) => Records.Remove(record);

    /// <summary>The interpreter's view of this scope's live work.</summary>
    public IReadOnlyCollection<BpmnLiveWork> ToLiveWork() =>
        Records.Select(x => new BpmnLiveWork(x.BindingRef, x.IterationId, x.Handle)).ToArray();
}
