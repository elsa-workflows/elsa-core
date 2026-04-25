namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Per-source options applied at <c>AddIngressSource&lt;T&gt;</c> time.
/// </summary>
public class IngressSourceRegistrationOptions
{
    /// <summary>
    /// Overrides <see cref="GracefulShutdownOptions.IngressPauseTimeout"/> for this source only.
    /// Leave null to use the global default.
    /// </summary>
    public TimeSpan? PauseTimeoutOverride { get; set; }
}
