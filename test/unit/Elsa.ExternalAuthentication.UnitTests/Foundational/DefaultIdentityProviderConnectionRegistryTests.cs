using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class DefaultIdentityProviderConnectionRegistryTests
{
    [Fact]
    public async Task ConfigurationConnectionsShadowDatabaseConnectionsWithTheSameKey()
    {
        var configuration = ExternalAuthenticationTestData.CreateConnection("configuration-oidc", ConnectionScope.HostTenantId, "oidc", isDefault: true);
        var database = ExternalAuthenticationTestData.CreateConnection("database-oidc", ConnectionScope.HostTenantId, "OIDC", isDefault: true);
        var registry = CreateRegistry(
            new TestConnectionSource("database", ConnectionSourceOwnership.Database, [(ConnectionScope.Host, [database])]),
            new TestConnectionSource("configuration", ConnectionSourceOwnership.Configuration, [(ConnectionScope.Host, [configuration])]));

        var result = await registry.GetAsync("tenant-a");

        var effective = Assert.Single(result.Connections, x => !x.IsShadowed);
        Assert.Equal("configuration-oidc", effective.Connection.Id);
        Assert.Single(result.Connections, x => x.IsShadowed);
        Assert.Equal(["configuration-oidc"], result.LoginMethods.Select(x => x.Id));
    }

    [Fact]
    public async Task RegistryUsesOnlyTheTargetTenantAndHostConnections()
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

        Assert.Equal(["host", "tenant-a"], result.Connections.Select(x => x.Connection.Id).OrderBy(x => x));
        Assert.DoesNotContain(result.Connections, x => x.Connection.Id == "tenant-b");
    }

    [Fact]
    public async Task DoesNotSelectAnAmbiguousTenantAndInheritedHostKey()
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

        Assert.All(result.Connections, x => Assert.Equal(ConnectionValidity.Invalid, x.Validity));
        Assert.Empty(result.LoginMethods);
        Assert.Null(byKey);
    }

    [Fact]
    public async Task RegistryOrdersLoginMethodsDeterministicallyAndUsesTenantDefault()
    {
        var hostDefault = ExternalAuthenticationTestData.CreateConnection("host-default", ConnectionScope.HostTenantId, "host", displayOrder: 20, isDefault: true);
        var tenantDefault = ExternalAuthenticationTestData.CreateConnection("tenant-default", "tenant-a", "tenant", displayOrder: 10, isDefault: true);
        var early = ExternalAuthenticationTestData.CreateConnection("early", "tenant-a", "early", displayOrder: 1);
        var disabled = ExternalAuthenticationTestData.CreateConnection("disabled", "tenant-a", "disabled", displayOrder: 0, isEnabled: false);
        var registry = CreateRegistry(new TestConnectionSource("database", ConnectionSourceOwnership.Database,
        [
            (ConnectionScope.Host, [hostDefault]),
            (new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), [tenantDefault, early, disabled])
        ]));

        var result = await registry.GetAsync("tenant-a");

        Assert.Equal(["early", "tenant-default", "host-default"], result.LoginMethods.Select(x => x.Id));
        Assert.Equal("tenant-default", Assert.Single(result.LoginMethods, x => x.IsDefault).Id);
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
