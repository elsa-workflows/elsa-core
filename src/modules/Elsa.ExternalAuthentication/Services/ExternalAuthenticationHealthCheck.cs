using Elsa.ExternalAuthentication.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>Opt-in health bridge. It reports the latest redacted test observation and never performs provider traffic.</summary>
public sealed class ExternalAuthenticationHealthCheck(
    IIdentityProviderConnectionRegistry registry,
    IConnectionObservationStore observations) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var registrySnapshot = await registry.GetAsync(string.Empty, cancellationToken);
        var enabled = registrySnapshot.Connections.Where(x => x.Connection.IsEnabled && x.Connection.ArchivedAt is null && !x.IsShadowed).ToArray();
        if (enabled.Length == 0)
            return HealthCheckResult.Healthy("No enabled external identity provider connections.");

        var latest = await Task.WhenAll(enabled.Select(x => observations.FindLatestAsync(x.Connection.Id, cancellationToken).AsTask()));
        if (latest.Any(x => x?.Status == Models.ConnectionObservationStatus.Failed))
            return HealthCheckResult.Degraded("One or more explicitly tested connections last failed.");
        return HealthCheckResult.Healthy("Latest explicit connection tests contain no failures.");
    }
}
