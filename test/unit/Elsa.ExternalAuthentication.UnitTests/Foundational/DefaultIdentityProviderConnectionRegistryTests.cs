using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class DefaultIdentityProviderConnectionRegistryTests
{
    [Fact]
    public async Task ConfigurationConnectionsShadowDatabaseConnectionsWithTheSameKey()
    {
        var configuration = ExternalAuthenticationTestData.CreateConnection("configuration-oidc", ConnectionScope.HostTenantId, "oidc", isPreferred: true);
        var database = ExternalAuthenticationTestData.CreateConnection("database-oidc", ConnectionScope.HostTenantId, "OIDC", isPreferred: true);
        var registry = CreateRegistry(
            new TestConnectionSource("database", ConnectionSourceOwnership.Database, [(ConnectionScope.Host, [database])]),
            new TestConnectionSource("configuration", ConnectionSourceOwnership.Configuration, [(ConnectionScope.Host, [configuration])]));

        var result = await registry.GetAsync("tenant-a");

        var effective = Assert.Single(result.Connections, x => !x.IsShadowed);
        Assert.Equal("configuration-oidc", effective.Connection.Id);
        Assert.Equal("database-oidc", Assert.Single(effective.Shadows).Id);
        var shadowed = Assert.Single(result.Connections, x => x.IsShadowed);
        Assert.Equal("configuration-oidc", Assert.IsType<IdentityProviderConnectionReference>(shadowed.ShadowedBy).Id);
        Assert.Equal(["configuration-oidc"], result.LoginMethods.Select(x => x.Id));
    }

    [Fact]
    public async Task ExplicitDatabaseOverrideIdentifiesItsShadowedConfigurationConnection()
    {
        var configuration = ExternalAuthenticationTestData.CreateConnection("configuration-oidc", ConnectionScope.HostTenantId, "oidc");
        var database = ExternalAuthenticationTestData.CreateConnection("database-oidc", ConnectionScope.HostTenantId, "OIDC");
        database.OverridesConfigurationConnection = true;
        var registry = CreateRegistry(
            new TestConnectionSource("database", ConnectionSourceOwnership.Database, [(ConnectionScope.Host, [database])]),
            new TestConnectionSource("configuration", ConnectionSourceOwnership.Configuration, [(ConnectionScope.Host, [configuration])]));

        var result = await registry.GetAsync("tenant-a");

        var effective = Assert.Single(result.Connections, x => !x.IsShadowed);
        Assert.Equal("database-oidc", effective.Connection.Id);
        Assert.Equal("configuration-oidc", Assert.Single(effective.Shadows).Id);
        var shadowed = Assert.Single(result.Connections, x => x.IsShadowed);
        var shadowedBy = Assert.IsType<IdentityProviderConnectionReference>(shadowed.ShadowedBy);
        Assert.Equal("database-oidc", shadowedBy.Id);
        Assert.Equal(ConnectionSourceOwnership.Database, shadowedBy.Ownership);
    }

    [Fact]
    public async Task ArchivedDatabaseOverrideDoesNotAppearInTheEffectiveConnectionShadows()
    {
        var configuration = ExternalAuthenticationTestData.CreateConnection("configuration-oidc", ConnectionScope.HostTenantId, "oidc");
        var archivedOverride = ExternalAuthenticationTestData.CreateConnection("database-oidc", ConnectionScope.HostTenantId, "OIDC");
        archivedOverride.OverridesConfigurationConnection = true;
        archivedOverride.ArchivedAt = DateTimeOffset.UtcNow;
        var registry = CreateRegistry(
            new TestConnectionSource("database", ConnectionSourceOwnership.Database, [(ConnectionScope.Host, [archivedOverride])]),
            new TestConnectionSource("configuration", ConnectionSourceOwnership.Configuration, [(ConnectionScope.Host, [configuration])]));

        var result = await registry.GetAsync("tenant-a");

        var effective = Assert.Single(result.Connections, x => !x.IsShadowed);
        Assert.Equal("configuration-oidc", effective.Connection.Id);
        Assert.Empty(effective.Shadows);
        Assert.Contains(result.Connections, x => x.Connection.Id == "database-oidc" && x.Connection.ArchivedAt.HasValue);
    }

    [Fact]
    public async Task ConfigurationPreferredConnectionWinsOverDatabasePreferredConnection()
    {
        var configuration = ExternalAuthenticationTestData.CreateConnection("configuration", ConnectionScope.HostTenantId, "configuration", displayOrder: 20, isPreferred: true);
        var database = ExternalAuthenticationTestData.CreateConnection("database", ConnectionScope.HostTenantId, "database", displayOrder: 1, isPreferred: true);
        var registry = CreateRegistry(
            new TestConnectionSource("database", ConnectionSourceOwnership.Database, [(ConnectionScope.Host, [database])]),
            new TestConnectionSource("configuration", ConnectionSourceOwnership.Configuration, [(ConnectionScope.Host, [configuration])]));

        var result = await registry.GetAsync("tenant-a");

        Assert.Equal("configuration", Assert.Single(result.LoginMethods, x => x.IsPreferred).Id);
    }

    [Fact]
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

        Assert.Equal(["host"], result.Connections.Select(x => x.Connection.Id));
        Assert.DoesNotContain(result.Connections, x => x.Connection.Id == "tenant-a");
        Assert.DoesNotContain(result.Connections, x => x.Connection.Id == "tenant-b");
    }

    [Fact]
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

        var effective = Assert.Single(result.Connections);
        Assert.Equal("host", effective.Connection.Id);
        Assert.Equal(ConnectionValidity.Unknown, effective.Validity);
        Assert.Equal("host", Assert.Single(result.LoginMethods).Id);
        Assert.Equal("host", Assert.IsType<EffectiveIdentityProviderConnection>(byKey).Connection.Id);
    }

    [Fact]
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

        Assert.Equal(["early", "preferred", "host"], result.LoginMethods.Select(x => x.Id));
        Assert.Equal("preferred", Assert.Single(result.LoginMethods, x => x.IsPreferred).Id);
        Assert.DoesNotContain(result.LoginMethods, x => x.Id == "disabled");
        Assert.Equal("/external-authentication/authorize/early", result.LoginMethods.First().InitiationUri.OriginalString);
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
