using Bpmn.Semantics;
using Elsa.Workflows;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// The outcome names a call activity's work reports for how its called workflow ended.
/// </summary>
/// <remarks>
/// A call activity is bound to a waiting <c>DispatchWorkflow</c>, which completes the same way whether the child finished,
/// faulted, was cancelled or was never started because its activation was denied, so a native flowchart carries on
/// regardless. BPMN does not: the interpreter routes a failed call through the call activity's error boundary, then the
/// scope's error event subprocess, and otherwise faults the process. It tells a failed call from a successful one only
/// by these outcome names.
/// </remarks>
internal static class BpmnCallActivityOutcomes
{
    /// <summary>The outcome names for a called workflow that was refused before it started, or ended with the given sub-status.</summary>
    /// <param name="childActivationDenied">Whether the child's activation was denied, so it never started.</param>
    /// <param name="childSubStatus">The child's final sub-status, or <c>null</c> when it is not known.</param>
    /// <returns>
    /// The interpreter's dispatch-failed outcome for a child whose activation was denied, and its failure outcome for a
    /// faulted or cancelled child; no outcome names otherwise, which routes the call activity's normal outgoing flow.
    /// </returns>
    public static IReadOnlyCollection<string> For(bool childActivationDenied, WorkflowSubStatus? childSubStatus)
    {
        if (childActivationDenied)
        {
            return [BpmnInterpreter.CallActivityDispatchFailedOutcomeName];
        }

        return childSubStatus switch
        {
            WorkflowSubStatus.Faulted => [BpmnInterpreter.CallActivityFaultedOutcomeName],
            WorkflowSubStatus.Cancelled => [BpmnInterpreter.CallActivityCancelledOutcomeName],
            _ => []
        };
    }
}
