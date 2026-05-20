namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Configures Elsa workflow runtime readiness health checks.
/// </summary>
public class ElsaReadinessHealthCheckOptions
{
    /// <summary>
    /// The maximum time the distributed-lock readiness probe waits to acquire its probe lock.
    /// </summary>
    public TimeSpan DistributedLockAcquisitionTimeout { get; set; } = TimeSpan.FromSeconds(1);
}
