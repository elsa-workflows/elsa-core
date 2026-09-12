using Elsa.Workflows;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

/// <summary>
/// The recording every stand-in work activity does, kept in one place so the activities differ only in what they do.
/// </summary>
internal static class BpmnTestWorkProbe
{
    /// <summary>Records that the work started, and what its scope reported as live at that moment.</summary>
    public static void RecordExecution(IActivity activity, BpmnTestLog? log, ActivityExecutionContext context)
    {
        log?.Record($"executed:{activity.Id}");
        log?.CaptureLiveWork($"liveWork@{activity.Id}", context);
    }

    /// <summary>
    /// Records that the work was torn down, and what was in the scheduler at that moment.
    /// </summary>
    /// <remarks>
    /// The scheduler snapshot is what makes command ordering observable: a teardown applied after the boundary path's
    /// <c>StartWork</c> sees the boundary path already scheduled, and one applied before it does not.
    /// </remarks>
    public static void RecordCancellation(IActivity activity, BpmnTestLog? log, SignalContext context)
    {
        var receiver = context.ReceiverActivityExecutionContext;

        // CancelSignal is delivered to the cancelled activity and then to each of its ancestors; only the first is
        // this activity's own cancellation.
        if (receiver.Activity != activity || !string.Equals(receiver.Id, context.SenderActivityExecutionContext.Id, StringComparison.Ordinal))
            return;

        log?.Record($"cancelled:{activity.Id}");
        log?.CaptureScheduled($"scheduled@cancel:{activity.Id}", receiver);
    }
}
