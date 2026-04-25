namespace Elsa.Workflows.Runtime;

/// <summary>
/// Observable state of an ingress source — the adapter through which external events enter the workflow engine.
/// </summary>
public enum IngressSourceState
{
    /// <summary>
    /// The source is delivering new work to the engine.
    /// </summary>
    Running,

    /// <summary>
    /// A pause request is in flight; the source has not yet acknowledged.
    /// </summary>
    Pausing,

    /// <summary>
    /// The source has acknowledged pause and is not delivering new work.
    /// </summary>
    Paused,

    /// <summary>
    /// A pause attempt threw, hung past the per-source timeout, or was detected as inconsistent (the source
    /// reported <see cref="Paused"/> but continued to deliver work).
    /// </summary>
    PauseFailed,

    /// <summary>
    /// A resume request is in flight; the source has not yet returned to <see cref="Running"/>.
    /// </summary>
    Resuming,

    /// <summary>
    /// A resume attempt failed. The source will not receive new work-start attempts until successfully resumed.
    /// </summary>
    ResumeFailed,
}
