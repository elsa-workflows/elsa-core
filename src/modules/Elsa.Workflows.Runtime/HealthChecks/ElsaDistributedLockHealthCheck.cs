using Elsa.Common;
using Elsa.Workflows.Runtime.Options;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.HealthChecks;

/// <summary>
/// Verifies that the configured distributed lock provider can acquire and release a probe lock.
/// </summary>
public class ElsaDistributedLockHealthCheck(
    IServiceProvider serviceProvider,
    IOptions<ElsaReadinessHealthCheckOptions> options,
    ILogger<ElsaDistributedLockHealthCheck> logger) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var distributedLockProvider = serviceProvider.GetService<IDistributedLockProvider>();
            if (distributedLockProvider == null)
            {
                return HealthCheckResult.Degraded("Elsa distributed lock provider is not registered.", data: new Dictionary<string, object>
                {
                    ["category"] = "distributed-locks"
                });
            }

            var lockName = $"elsa-health-check-{Guid.NewGuid():N}";
            await using var handle = await distributedLockProvider.TryAcquireLockAsync(lockName, options.Value.DistributedLockAcquisitionTimeout, cancellationToken);
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (!e.IsFatal())
        {
            logger.LogWarning(e, "Elsa distributed lock provider is not reachable.");
            return HealthCheckResult.Unhealthy("Elsa distributed lock provider is not reachable.", data: new Dictionary<string, object>
            {
                ["category"] = "distributed-locks"
            });
        }
    }
}
