using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class DefaultIdentityProviderConnectionRegistryTests
{
    [Test]
    public async Task ConfigurationConnectionsShadowDatabaseConnectionsWithTheSameKey()
    {
        var configuration = ExternalAuthenticationTestData.CreateConnection("configuration-oidc", ConnectionScope.HostTenantId, "oidc", isPreferred: true);
        var database = ExternalAuthenticationTestData.CreateConnection("database-oidc", ConnectionScope.HostTenantId, "OIDC", isPreferred: true);
        var registry = CreateRegistry(
            new TestConnectionSource("database", ConnectionSourceOwnership.Database, [(ConnectionScope.Host, [database])]),
            new TestConnectionSource("configuration", ConnectionSourceOwnership.Configuration, [(ConnectionScope.Host, [configuration])]));

        var result = await registry.GetAsync("tenant-a");

        var effective = await Assert.That(result.Connections).HasSingleItem(x => !x.IsShadowed);
        await Assert.That(effective.Connection.Id).IsEqualTo("configuration-oidc");
        var effectiveShadow = await Assert.That(effective.Shadows).HasSingleItem();
        await Assert.That(effectiveShadow.Id).IsEqualTo("database-oidc");
        var shadowed = await Assert.That(result.Connections).HasSingleItem(x => x.IsShadowed);
        var shadowedBy = await Assert.That(shadowed.ShadowedBy).IsNotNull();
        await Assert.That(shadowedBy).IsOfType(typeof(IdentityProviderConnectionReference));
        await Assert.That(shadowedBy.Id).IsEqualTo("configuration-oidc");
        await Assert.That(result.LoginMethods.Select(x => x.Id)).IsEquivalentTo(["configuration-oidc"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task ExplicitDatabaseOverrideIdentifiesItsShadowedConfigurationConnection()
    {
        var configuration = ExternalAuthenticationTestData.CreateConnection("configuration-oidc", ConnectionScope.HostTenantId, "oidc");
        var database = ExternalAuthenticationTestData.CreateConnection("database-oidc", ConnectionScope.HostTenantId, "OIDC");
        database.OverridesConfigurationConnection = true;
        var registry = CreateRegistry(
            new TestConnectionSource("database", ConnectionSourceOwnership.Database, [(ConnectionScope.Host, [database])]),
            new TestConnectionSource("configuration", ConnectionSourceOwnership.Configuration, [(ConnectionScope.Host, [configuration])]));

        var result = await registry.GetAsync("tenant-a");

        var effective = await Assert.That(result.Connections).HasSingleItem(x => !x.IsShadowed);
        await Assert.That(effective.Connection.Id).IsEqualTo("database-oidc");
        var effectiveShadow = await Assert.That(effective.Shadows).HasSingleItem();
        await Assert.That(effectiveShadow.Id).IsEqualTo("configuration-oidc");
        var shadowed = await Assert.That(result.Connections).HasSingleItem(x => x.IsShadowed);
        var shadowedBy = await Assert.That(shadowed.ShadowedBy).IsNotNull();
        await Assert.That(shadowedBy).IsOfType(typeof(IdentityProviderConnectionReference));
        await Assert.That(shadowedBy.Id).IsEqualTo("database-oidc");
        await Assert.That(shadowedBy.Ownership).IsEqualTo(ConnectionSourceOwnership.Database);
    }

    [Test]
    public async Task ArchivedDatabaseOverrideDoesNotParticipateInActiveShadowRelationships()
    {
        var configuration = ExternalAuthenticationTestData.CreateConnection("configuration-oidc", ConnectionScope.HostTenantId, "oidc", displayOrder: 10);
        var archivedOverride = ExternalAuthenticationTestData.CreateConnection("database-oidc", ConnectionScope.HostTenantId, "OIDC", displayOrder: 0);
        archivedOverride.OverridesConfigurationConnection = true;
        archivedOverride.ArchivedAt = DateTimeOffset.UtcNow;
        var registry = CreateRegistry(
            new TestConnectionSource("database", ConnectionSourceOwnership.Database, [(ConnectionScope.Host, [archivedOverride])]),
            new TestConnectionSource("configuration", ConnectionSourceOwnership.Configuration, [(ConnectionScope.Host, [configuration])]));

        var result = await registry.GetAsync("tenant-a");
        var resolved = await registry.FindByKeyAsync("tenant-a", "oidc");

        var effective = await Assert.That(result.Connections).HasSingleItem(x => !x.Connection.ArchivedAt.HasValue && !x.IsShadowed);
        await Assert.That(effective.Connection.Id).IsEqualTo("configuration-oidc");
        var resolvedConnection = await Assert.That(resolved).IsNotNull();
        await Assert.That(resolvedConnection).IsOfType(typeof(EffectiveIdentityProviderConnection));
        await Assert.That(resolvedConnection.Connection.Id).IsEqualTo("configuration-oidc");
        await Assert.That(effective.Shadows).IsEmpty();
        var archived = await Assert.That(result.Connections).HasSingleItem(x => x.Connection.Id == "database-oidc");
        await Assert.That(archived.Connection.ArchivedAt.HasValue).IsTrue();
        await Assert.That(archived.IsShadowed).IsFalse();
        await Assert.That(archived.ShadowedBy).IsNull();
        await Assert.That(archived.Shadows).IsEmpty();
    }

    [Test]
    public async Task ConfigurationPreferredConnectionWinsOverDatabasePreferredConnection()
    {
        var configuration = ExternalAuthenticationTestData.CreateConnection("configuration", ConnectionScope.HostTenantId, "configuration", displayOrder: 20, isPreferred: true);
        var database = ExternalAuthenticationTestData.CreateConnection("database", ConnectionScope.HostTenantId, "database", displayOrder: 1, isPreferred: true);
        var registry = CreateRegistry(
            new TestConnectionSource("database", ConnectionSourceOwnership.Database, [(ConnectionScope.Host, [database])]),
            new TestConnectionSource("configuration", ConnectionSourceOwnership.Configuration, [(ConnectionScope.Host, [configuration])]));

        var result = await registry.GetAsync("tenant-a");

        var preferredLoginMethod = await Assert.That(result.LoginMethods).HasSingleItem(x => x.IsPreferred);
        await Assert.That(preferredLoginMethod.Id).IsEqualTo("configuration");
    }

    [Test]
    public async Task RegistryUsesOnlyHostConnections()
    {
        var host = ExternalAuthenticationTestData.CreateConnection("host", ConnectionScope.HostTenantId, "host");
        var tenantA = ExternalAuthenticationTestData.CreateConnection("tenant-a", "tenant-a", "tenant-a");
        var tenantB = ExternalAuthenticationTestData.CreateConnection("tenant-b", "tenant-b", "tenant-b");
        var registry = CreateRegistry(new TestConnectionSource("database", ConnectionSourceOwnership.Database,
        [
            (ConnectionScope.Host, [host]),
            (new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), [tenantA]),
            (new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-b"), [tenantB])
        ]));

        var result = await registry.GetAsync("tenant-a");

        await Assert.That(result.Connections.Select(x => x.Connection.Id)).IsEquivalentTo(["host"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(result.Connections).DoesNotContain(x => x.Connection.Id == "tenant-a");
        await Assert.That(result.Connections).DoesNotContain(x => x.Connection.Id == "tenant-b");
    }

    [Test]
    public async Task IgnoresLegacyTenantConnectionThatSharesAHostKey()
    {
        var host = ExternalAuthenticationTestData.CreateConnection("host", ConnectionScope.HostTenantId, "contoso");
        var tenant = ExternalAuthenticationTestData.CreateConnection("tenant", "tenant-a", "CONTOSO");
        var registry = CreateRegistry(new TestConnectionSource("database", ConnectionSourceOwnership.Database,
        [
            (ConnectionScope.Host, [host]),
            (new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), [tenant])
        ]));

        var result = await registry.GetAsync("tenant-a");
        var byKey = await registry.FindByKeyAsync("tenant-a", "contoso");

        var effective = await Assert.That(result.Connections).HasSingleItem();
        await Assert.That(effective.Connection.Id).IsEqualTo("host");
        await Assert.That(effective.Validity).IsEqualTo(ConnectionValidity.Unknown);
        var loginMethod = await Assert.That(result.LoginMethods).HasSingleItem();
        var resolvedConnection = await Assert.That(byKey).IsNotNull();
        await Assert.That(resolvedConnection).IsOfType(typeof(EffectiveIdentityProviderConnection));
        await Assert.That(loginMethod.Id).IsEqualTo("host");
        await Assert.That(resolvedConnection.Connection.Id).IsEqualTo("host");
    }

    [Test]
    public async Task RegistryOrdersHostLoginMethodsDeterministicallyAndUsesPreferredConnection()
    {
        var host = ExternalAuthenticationTestData.CreateConnection("host", ConnectionScope.HostTenantId, "host", displayOrder: 20);
        var preferred = ExternalAuthenticationTestData.CreateConnection("preferred", ConnectionScope.HostTenantId, "preferred", displayOrder: 10, isPreferred: true);
        var early = ExternalAuthenticationTestData.CreateConnection("early", ConnectionScope.HostTenantId, "early", displayOrder: 1);
        var disabled = ExternalAuthenticationTestData.CreateConnection("disabled", ConnectionScope.HostTenantId, "disabled", displayOrder: 0, isEnabled: false);
        var registry = CreateRegistry(new TestConnectionSource("database", ConnectionSourceOwnership.Database,
        [
            (ConnectionScope.Host, [host, preferred, early, disabled])
        ]));

        var result = await registry.GetAsync("tenant-a");

        await Assert.That(result.LoginMethods.Select(x => x.Id)).IsEquivalentTo(["early", "preferred", "host"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        var preferredLoginMethod = await Assert.That(result.LoginMethods).HasSingleItem(x => x.IsPreferred);
        await Assert.That(preferredLoginMethod.Id).IsEqualTo("preferred");
        await Assert.That(result.LoginMethods).DoesNotContain(x => x.Id == "disabled");
        await Assert.That(result.LoginMethods.First().InitiationUri.OriginalString).IsEqualTo("/external-authentication/authorize/early");
    }

    private static DefaultIdentityProviderConnectionRegistry CreateRegistry(params IIdentityProviderConnectionSource[] sources) => new(sources, new ConnectionRevisionCalculator());

    private sealed class TestConnectionSource(
        string name,
        ConnectionSourceOwnership ownership,
        IReadOnlyCollection<(ConnectionScope Scope, IReadOnlyCollection<IdentityProviderConnection> Connections)> snapshots) : IIdentityProviderConnectionSource
    {
        public string Name => name;
        public ConnectionSourceOwnership Ownership => ownership;

        public ValueTask<ConnectionSourceSnapshot> GetSnapshotAsync(ConnectionScope scope, CancellationToken cancellationToken = default)
        {
            var connections = snapshots.FirstOrDefault(x => x.Scope == scope).Connections ?? [];
            return ValueTask.FromResult(new ConnectionSourceSnapshot(scope, $"{name}-{scope.Kind}-{scope.TenantId}", connections));
        }
    }
}
