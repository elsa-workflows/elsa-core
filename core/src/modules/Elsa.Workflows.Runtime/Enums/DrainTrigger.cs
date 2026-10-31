namespace Elsa.Workflows.Runtime;

/// <summary>
/// Identifies what caused a drain to be initiated.
/// </summary>
public enum DrainTrigger
{
    /// <summary>The host process received a stop signal (e.g., SIGTERM, Ctrl+C, orchestrator rollout).</summary>
    HostStopSignal,

    /// <summary>The shell platform is deactivating the shell that owns this runtime.</summary>
    ShellDeactivation,

    /// <summary>An operator invoked the admin force endpoint.</summary>
    OperatorForce,
}
