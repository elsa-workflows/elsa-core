using System.Text.Json;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.IntegrationTests.Fixtures.ConformanceExtensions;

public class ConformanceExtensionTests
{
    [Fact]
    public async Task ProviderPolicyAndGrantExtensionsUseTheProtocolNeutralEnvelope()
    {
        var adapter = new ConformanceExternalAuthenticationAdapter();
        var options = new ExternalAuthenticationOptions
        {
            AllowedAdapterTypes = [adapter.Type],
            AllowedUnlinkedIdentityPolicyTypes = [ConformanceUnlinkedIdentityPolicy.PolicyType],
            AllowedPermissionGrantSourceTypes = [ConformancePermissionGrantSource.SourceType]
        };
        var validator = new ExtensionDescriptorValidator();
        var adapterRegistry = new DefaultExternalAuthenticationAdapterRegistry(
            [adapter],
            validator,
            Microsoft.Extensions.Options.Options.Create(options));
        var policyRegistry = new DefaultUnlinkedIdentityPolicyRegistry(
            [new ConformanceUnlinkedIdentityPolicy()],
            validator,
            Microsoft.Extensions.Options.Options.Create(options));
        var grantRegistry = new DefaultPermissionGrantSourceRegistry(
            [new ConformancePermissionGrantSource()],
            validator,
            Microsoft.Extensions.Options.Options.Create(options));
        var migrationService = new AdapterSettingsMigrationService(
            adapterRegistry,
            [new ConformanceAdapterSettingsMigration()]);
        var migrated = await migrationService.MigrateAsync(
            adapter.Type,
            1,
            JsonSerializer.SerializeToElement(new { authority = "https://issuer.example" }));
        var connection = new IdentityProviderConnection
        {
            Id = "connection-a",
            TenantId = "tenant-a",
            Key = "contoso",
            AdapterType = adapter.Type,
            AdapterSettingsVersion = migrated.SettingsVersion,
            AdapterSettings = migrated.Settings,
            DisplayName = "Contoso",
            IsEnabled = true,
            MaterialRevision = "revision-a"
        };
        var effective = new EffectiveIdentityProviderConnection(
            connection,
            ConnectionSourceOwnership.Configuration,
            new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"),
            ConnectionValidity.Valid,
            false,
            "configuration");
        var transaction = new BrokerTransaction
        {
            HandleHash = "hash",
            ClientId = "studio",
            CallbackUri = new Uri("https://studio.example/callback"),
            ReturnPath = "/",
            TenantId = "tenant-a",
            ConnectionId = connection.Id,
            ConnectionMaterialRevision = connection.MaterialRevision,
            PkceChallenge = "challenge",
            ExpiresAt = DateTimeOffset.Parse("2026-07-24T13:00:00Z")
        };
        var authentication = await adapter.AuthenticateCallbackAsync(new ExternalCallbackContext(
            effective,
            new Dictionary<string, ResolvedSecretBinding>(),
            transaction,
            "state",
            new Dictionary<string, IReadOnlyCollection<string>> { ["subject"] = ["subject-a"] },
            new TestClock()));
        Assert.True(policyRegistry.TryGet(ConformanceUnlinkedIdentityPolicy.PolicyType, out var policy));
        var decision = await policy.EvaluateAsync(new UnlinkedIdentityContext(
            "tenant-a",
            effective,
            authentication.Identity,
            authentication.ProjectedClaims,
            default));
        Assert.True(grantRegistry.TryGet(ConformancePermissionGrantSource.SourceType, out var source));
        var grants = await source.GetGrantsAsync(new PermissionGrantContext(
            "tenant-a",
            "user-a",
            effective,
            authentication.Identity,
            authentication.ProjectedClaims,
            new GrantSourceSelection(source.Type, 1, default, 0)));

        Assert.Equal(2, connection.AdapterSettingsVersion);
        Assert.Equal("https://issuer.example", authentication.Identity.Issuer);
        Assert.Equal("subject-a", authentication.Identity.Subject);
        Assert.IsType<UnlinkedIdentityDecision.LinkExistingUser>(decision);
        Assert.Equal("conformance:read", Assert.Single(grants.Grants).Permission);
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-07-24T12:00:00Z");
    }
}
