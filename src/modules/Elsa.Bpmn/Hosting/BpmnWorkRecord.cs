namespace Elsa.Bpmn.Hosting;

/// <summary>
/// One unit of work a BPMN scope started and has not finished: the interpreter's view of it, plus the identity of
/// the Elsa activity execution that runs it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ChildContextId"/> is the whole point of this record. A unit of work is keyed on the child
/// <see cref="Elsa.Workflows.ActivityExecutionContext"/>'s id and never on <c>ActivityExecutionContext.Tag</c>.
/// Tag is shared mutable state: the completion-callback dispatch writes the popped callback entry's tag onto the
/// <i>receiving</i> context, so a nested scope's own tag is rewritten by its own children, and a parent that
/// remembered a child as one tag finds the child wearing another. Making the handles globally unique does not fix
/// that, because the two values are written by different owners at different times.
/// </para>
/// </remarks>
internal sealed class BpmnWorkRecord
{
    /// <summary>The opaque host handle the interpreter echoes back. Scope-local and minted by <see cref="BpmnWorkLedger"/>.</summary>
    public string Handle { get; set; } = null!;

    /// <summary>The binding ref the interpreter asked to start.</summary>
    public string BindingRef { get; set; } = null!;

    /// <summary>The multi-instance iteration id this unit runs under, or <c>null</c> for ordinary single-run work.</summary>
    public string? IterationId { get; set; }

    /// <summary>The element the work belongs to, for reporting.</summary>
    public string ElementId { get; set; } = null!;

    /// <summary>The id of the child <see cref="Elsa.Workflows.ActivityExecutionContext"/> running this work.</summary>
    public string ChildContextId { get; set; } = null!;
}
