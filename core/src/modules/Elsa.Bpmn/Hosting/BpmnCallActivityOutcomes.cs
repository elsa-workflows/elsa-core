using Bpmn.Semantics;
using Elsa.Workflows;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// The outcome names a call activity's work reports for how its called workflow ended.
/// </summary>
/// <remarks>
/// A call activity is bound to a waiting <c>DispatchWorkflow</c>, which completes the same way whether the child finished,
/// faulted or was cancelled, so a native flowchart carries on regardless. BPMN does not: the interpreter routes a failed
/// call through the call activity's error boundary, then the scope's error event subprocess, and otherwise faults the
/// process. It tells a failed call from a successful one only by these outcome names.
/// </remarks>
internal static class BpmnCallActivityOutcomes
{
    /// <summary>The outcome names for a called workflow that ended with the given sub-status.</summary>
    /// <param name="childSubStatus">The child's final sub-status, or <c>null</c> when it is not known.</param>
    /// <returns>
    /// The interpreter's failure outcome for a faulted or cancelled child; no outcome names otherwise, which routes the
    /// call activity's normal outgoing flow.
    /// </returns>
    public static IReadOnlyCollection<string> For(WorkflowSubStatus? childSubStatus) => childSubStatus switch
    {
        WorkflowSubStatus.Faulted => [BpmnInterpreter.CallActivityFaultedOutcomeName],
        WorkflowSubStatus.Cancelled => [BpmnInterpreter.CallActivityCancelledOutcomeName],
        _ => []
    };
}
