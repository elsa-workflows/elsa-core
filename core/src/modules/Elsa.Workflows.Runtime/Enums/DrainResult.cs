namespace Elsa.Workflows.Runtime;

/// <summary>
/// Overall result reported by a completed drain.
/// </summary>
public enum DrainResult
{
    /// <summary>All active execution cycles reached a natural persistence boundary within the drain deadline.</summary>
    CompletedWithinDeadline,

    /// <summary>The drain deadline elapsed while execution cycles were still active; they were force-cancelled and marked Interrupted.</summary>
    DeadlineExceeded,

    /// <summary>An operator invoked the force endpoint — drain completed with zero deadline, cancelling all active execution cycles.</summary>
    Forced,

    /// <summary>An unexpected exception escaped the drain protocol. Execution cycles already marked Interrupted remain so; recovery relies on the timeout-based path.</summary>
    AbortedByUnhandledException,
}
