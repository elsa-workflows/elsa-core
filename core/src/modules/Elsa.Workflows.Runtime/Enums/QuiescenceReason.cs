namespace Elsa.Workflows.Runtime;

/// <summary>
/// Describes why the workflow runtime is not accepting new work. Values are composable:
/// a runtime may be draining AND administratively paused simultaneously.
/// </summary>
[Flags]
public enum QuiescenceReason
{
    /// <summary>
    /// The runtime is accepting new work normally.
    /// </summary>
    None = 0,

    /// <summary>
    /// An operator has placed the runtime into a reversible paused state. Cleared by an explicit resume.
    /// </summary>
    AdministrativePause = 1 << 0,

    /// <summary>
    /// The runtime is draining in preparation for termination (host stop or shell deactivation).
    /// Forward-only within a runtime generation: once set, cleared only by a new generation.
    /// </summary>
    Drain = 1 << 1,
}
