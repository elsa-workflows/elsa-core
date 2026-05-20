using Elsa.Common;
using Medallion.Threading;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elsa.Workflows.Runtime.HealthChecks;

/// <summary>
/// Verifies that the configured distributed lock provider can acquire and release a probe lock.
/// </summary>
public class ElsaDistributedLockHealthCheck(IDistributedLockProvider distributedLockProvider) : IHealthCheck
{
    private const string LockName = "elsa-health-check";
    private static readonly TimeSpan LockAcquisitionTimeout = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var handle = await distributedLockProvider.TryAcquireLockAsync(LockName, LockAcquisitionTimeout, cancellationToken);
            if (handle == null)
            {
                return HealthCheckResult.Degraded("Elsa distributed lock provider was reachable, but the probe lock was not acquired.", data: new Dictionary<string, object>
                {
                    ["category"] = "distributed-locks"
                });
            }

            return HealthCheckResult.Healthy("Elsa distributed lock provider is reachable.", new Dictionary<string, object>
            {
                ["category"] = "distributed-locks"
            });
        }
        catch (Exception e) when (!e.IsFatal())
        {
            return HealthCheckResult.Unhealthy("Elsa distributed lock provider is not reachable.", e, new Dictionary<string, object>
            {
                ["category"] = "distributed-locks"
            });
        }
    }
}
