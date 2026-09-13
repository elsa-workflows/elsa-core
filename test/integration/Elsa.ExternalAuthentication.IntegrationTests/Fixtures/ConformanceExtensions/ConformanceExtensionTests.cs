using System.Text.Json;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.IntegrationTests.Fixtures.ConformanceExtensions;

public class ConformanceExtensionTests
{
    [Test]
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
            [],
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
            TenantId = ConnectionScope.HostTenantId,
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
            ConnectionScope.Host,
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
        await Assert.That(policyRegistry.TryGet(ConformanceUnlinkedIdentityPolicy.PolicyType, out var policy)).IsTrue();
        var decision = await policy.EvaluateAsync(new UnlinkedIdentityContext(
            "tenant-a",
            effective,
            authentication.Identity,
            authentication.ProjectedClaims,
            default));
        await Assert.That(grantRegistry.TryGet(ConformancePermissionGrantSource.SourceType, out var source)).IsTrue();
        var grants = await source.GetGrantsAsync(new PermissionGrantContext(
            "tenant-a",
            "user-a",
            effective,
            authentication.Identity,
            authentication.ProjectedClaims,
            new GrantSourceSelection(source.Type, 1, default, 0)));

        await Assert.That(connection.AdapterSettingsVersion).IsEqualTo(2);
        await Assert.That(authentication.Identity.Issuer).IsEqualTo("https://issuer.example");
        await Assert.That(authentication.Identity.Subject).IsEqualTo("subject-a");
        await Assert.That(decision).IsOfType(typeof(UnlinkedIdentityDecision.LinkExistingUser));
        var grant = (await Assert.That(grants.Grants).HasSingleItem())!;
        await Assert.That(grant.Permission).IsEqualTo("conformance:read");
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-07-24T12:00:00Z");
    }
}
