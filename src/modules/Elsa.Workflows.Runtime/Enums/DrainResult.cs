namespace Elsa.Workflows.Runtime;

/// <summary>
/// Overall result reported by a completed drain.
/// </summary>
public enum DrainResult
{
    /// <summary>All active bursts reached a natural persistence boundary within the drain deadline.</summary>
    CompletedWithinDeadline,

    /// <summary>The drain deadline elapsed while bursts were still active; they were force-cancelled and marked Interrupted.</summary>
    DeadlineExceeded,

    /// <summary>An operator invoked the force endpoint — drain completed with zero deadline, cancelling all active bursts.</summary>
    Forced,

    /// <summary>An unexpected exception escaped the drain protocol. Bursts already marked Interrupted remain so; recovery relies on the timeout-based path.</summary>
    AbortedByUnhandledException,
}
