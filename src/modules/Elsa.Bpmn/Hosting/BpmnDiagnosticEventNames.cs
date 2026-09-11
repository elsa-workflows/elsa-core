namespace Elsa.Bpmn.Hosting;

/// <summary>
/// The stable execution log <c>eventName</c> for each <c>Bpmn.Model.State.BpmnDiagnosticKind</c>, and the
/// <c>source</c> every one of them carries. <see cref="BpmnScopeHost"/> derives the event name directly from the
/// diagnostic kind's own enum member name; the constants here exist so Studio has one place to mirror them rather
/// than depending on the library's integer enum values.
/// </summary>
public static class BpmnDiagnosticEventNames
{
    /// <summary>The <c>source</c> every execution log entry projected from a BPMN diagnostic carries.</summary>
    public const string Source = "BPMN";

    /// <summary>
    /// A token arrived at an element via a sequence flow. The scope's own start — the initial token, which carries
    /// no flow — is also this kind but, like <see cref="Completed"/>, is never projected by
    /// <see cref="BpmnScopeHost"/>, because the scope's own activity lifecycle already journals it.
    /// </summary>
    public const string TokenEmitted = "TokenEmitted";

    /// <summary>An element started bound work: a single unit, or one instance of a multi-instance loop.</summary>
    public const string Scheduled = "Scheduled";

    /// <summary>A token arrived at a join and is waiting for its siblings.</summary>
    public const string Waiting = "Waiting";

    /// <summary>A join fired after its arrivals were satisfied.</summary>
    public const string Joined = "Joined";

    /// <summary>An end event consumed a token, or a multi-instance loop consumed a finished instance's token.</summary>
    public const string Consumed = "Consumed";

    /// <summary>A unit of work was cancelled.</summary>
    public const string Canceled = "Canceled";

    /// <summary>A terminate end event ended the process.</summary>
    public const string Terminated = "Terminated";

    /// <summary>An element's behavior failed.</summary>
    public const string BehaviorFailure = "BehaviorFailure";

    /// <summary>
    /// The scope itself finished. Never projected by <see cref="BpmnScopeHost"/>: unlike every other kind, it names
    /// no element, and the scope's own activity lifecycle already journals its completion.
    /// </summary>
    public const string Completed = "Completed";

    /// <summary>A unit of work faulted.</summary>
    public const string Faulted = "Faulted";

    /// <summary>A host completion carrying an attached compensation boundary registered a compensable.</summary>
    public const string CompensationRegistered = "CompensationRegistered";

    /// <summary>A compensate throw/end event triggered a compensation replay.</summary>
    public const string CompensationTriggered = "CompensationTriggered";

    /// <summary>A compensation handler ran to completion for one registered compensable.</summary>
    public const string Compensated = "Compensated";

    /// <summary>A cancel end event began (or completed) cancelling a transaction scope.</summary>
    public const string TransactionCancelled = "TransactionCancelled";

    /// <summary>An escalation throw/end event staged an enclosing-scope signal notification.</summary>
    public const string EscalationRaised = "EscalationRaised";

    /// <summary>An escalation notification matched an attached boundary and fired it.</summary>
    public const string EscalationCaught = "EscalationCaught";

    /// <summary>An escalation reached a scope that could not catch it; a no-op, never a fault.</summary>
    public const string EscalationUnhandled = "EscalationUnhandled";

    /// <summary>An interrupting escalation boundary matched a notification whose host had already terminalized; a no-op, never a fault.</summary>
    public const string EscalationLate = "EscalationLate";

    /// <summary>An event subprocess was activated by its start-event trigger.</summary>
    public const string EventSubprocessActivated = "EventSubprocessActivated";

    /// <summary>An event subprocess body ran to completion.</summary>
    public const string EventSubprocessCompleted = "EventSubprocessCompleted";

    /// <summary>A call activity's bound child failed and the engine routed the call-activity failure ladder instead of normal outbound flows.</summary>
    public const string CallActivityFailureRouted = "CallActivityFailureRouted";

    /// <summary>A message, signal or timer triggered scope listener was armed.</summary>
    public const string ScopeListenerArmed = "ScopeListenerArmed";

    /// <summary>A message, signal or timer triggered scope listener fired.</summary>
    public const string ScopeListenerFired = "ScopeListenerFired";

    /// <summary>A message, signal or timer triggered scope listener was retired.</summary>
    public const string ScopeListenerRetired = "ScopeListenerRetired";
}
