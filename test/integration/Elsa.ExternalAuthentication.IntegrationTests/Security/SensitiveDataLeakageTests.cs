using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elsa.ExternalAuthentication.IntegrationTests.Security;

/// <summary>
/// Contract tests for values that must never cross the broker boundary.  The marker values are deliberately
/// distinctive so a future addition to a public DTO, redirect, health detail, or notification is caught here.
/// </summary>
public sealed class SensitiveDataLeakageTests
{
    private const string Secret = "secret-value-must-not-leak";
    private const string AccessToken = "access-token-must-not-leak";
    private const string RefreshToken = "refresh-token-must-not-leak";
    private const string ProviderBody = "provider-response-must-not-leak";
    private const string RawClaim = "raw-claim-must-not-leak";

    [Fact]
    public void PublicErrorsAndRedirectsContainOnlySafeCategoryAndCorrelationData()
    {
        var error = BrokerErrorFactory.Create(BrokerErrorCategory.AuthenticationFailed, $"{Secret}?{AccessToken}");
        var redirect = new Uri($"https://studio.example/authentication/external/callback?error={error.Error}&correlation_id={error.CorrelationId}");
        var serialized = JsonSerializer.Serialize(new { error, redirect = redirect.AbsoluteUri });

        Assert.DoesNotContain(Secret, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(ProviderBody, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(RawClaim, serialized, StringComparison.Ordinal);
        Assert.Equal(32, error.CorrelationId.Length);
    }

    [Fact]
    public void RedactionBoundaryRemovesRawClaimsAndMasksConfiguredProjectedClaims()
    {
        var projected = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            ["email"] = [RawClaim],
            ["groups"] = ["operators"]
        };

        var raw = ExternalAuthenticationRedactor.RedactRawClaims(projected);
        var redacted = ExternalAuthenticationRedactor.RedactProjectedClaims(projected, new HashSet<string>(["email"], StringComparer.Ordinal));
        var serialized = JsonSerializer.Serialize(new
        {
            secret = ExternalAuthenticationRedactor.RedactSecret(Secret),
            token = ExternalAuthenticationRedactor.RedactToken(AccessToken),
            providerBody = ExternalAuthenticationRedactor.RedactProviderResponseBody(ProviderBody),
            raw,
            redacted
        });

        Assert.Empty(raw);
        Assert.Equal([ExternalAuthenticationRedactor.RedactedValue], redacted["email"]);
        Assert.Equal(["operators"], redacted["groups"]);
        Assert.DoesNotContain(Secret, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(ProviderBody, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(RawClaim, serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void SecurityNotificationsSerializeOnlyTheirExplicitSafeFields()
    {
        var notification = new ExternalAuthenticationOutcomeRecorded(
            new SecurityEventContext("admin", "tenant-a", "connection-a", "user-a", DateTimeOffset.UtcNow, SecurityEventOutcome.Rejected, "correlation-a", "Authentication could not be completed."),
            "external",
            "callback",
            "authentication_failed");
        var serialized = JsonSerializer.Serialize(notification);

        Assert.DoesNotContain(Secret, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(RefreshToken, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(ProviderBody, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(RawClaim, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("subject", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("claim", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthDetailsExposeOnlyTheFixedSafeSummaryOfExplicitTests()
    {
        var connection = new IdentityProviderConnection { Id = "connection-a", TenantId = ConnectionScope.DefaultTenantId, Key = "contoso", AdapterType = "test", DisplayName = "Contoso", IsEnabled = true, MaterialRevision = "revision-a" };
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.DefaultTenant, ConnectionValidity.Valid, false, "configuration");
        var registry = new FixedRegistry(effective);
        var observations = new InMemoryConnectionObservationStore();
        await observations.SaveLatestAsync(new ConnectionObservation(connection.Id, connection.MaterialRevision, DateTimeOffset.UtcNow, ConnectionObservationStatus.Failed, "temporarily_unavailable", TimeSpan.Zero, $"{Secret} {AccessToken} {ProviderBody}", [$"{RawClaim}"], "correlation-a"));

        var result = await new ExternalAuthenticationHealthCheck(registry, observations).CheckHealthAsync(new HealthCheckContext());
        var serialized = JsonSerializer.Serialize(new { result.Status, result.Description, result.Data });

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.DoesNotContain(Secret, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessToken, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(ProviderBody, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(RawClaim, serialized, StringComparison.Ordinal);
    }

    private sealed class FixedRegistry(EffectiveIdentityProviderConnection connection) : IIdentityProviderConnectionRegistry
    {
        public ValueTask<EffectiveConnectionRegistry> GetAsync(string targetTenantId, CancellationToken cancellationToken = default) => ValueTask.FromResult(new EffectiveConnectionRegistry([connection], [], "v1"));
        public ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(string targetTenantId, string key, CancellationToken cancellationToken = default) => ValueTask.FromResult<EffectiveIdentityProviderConnection?>(connection);
        public ValueTask<EffectiveIdentityProviderConnection?> FindByIdAsync(string targetTenantId, string connectionId, CancellationToken cancellationToken = default) => ValueTask.FromResult<EffectiveIdentityProviderConnection?>(connection);
    }
}
