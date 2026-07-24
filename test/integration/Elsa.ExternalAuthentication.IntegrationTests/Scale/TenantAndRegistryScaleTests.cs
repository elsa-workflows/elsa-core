using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.IntegrationTests.Scale;

public sealed class TenantAndRegistryScaleTests
{
    [Fact]
    public async Task TenantFuzzingNeverLeaksAnotherTenantsConnectionAndHostCollisionsAreInvalid()
    {
        var source = new StaticSource("configuration", ConnectionSourceOwnership.Configuration, new Dictionary<ConnectionScope, IReadOnlyCollection<IdentityProviderConnection>>
        {
            [ConnectionScope.Host] = [Connection("host", ConnectionScope.HostTenantId, "shared")],
            [new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a")] = [Connection("tenant-a", "tenant-a", "only-a")],
            [new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-b")] = [Connection("tenant-b", "tenant-b", "only-b")]
        });
        var registry = new DefaultIdentityProviderConnectionRegistry([source], new ConnectionRevisionCalculator());

        foreach (var tenantId in new[] { "tenant-a", "tenant-b", "tenant-c", string.Empty })
        {
            var snapshot = await registry.GetAsync(tenantId);
            Assert.DoesNotContain(snapshot.Connections, x => x.Connection.TenantId is "tenant-a" or "tenant-b" && x.Connection.TenantId != tenantId);
        }

        source.Add(new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), Connection("collision", "tenant-a", "shared"));
        var collision = await registry.GetAsync("tenant-a");
        Assert.All(collision.Connections.Where(x => x.Connection.Key == "shared"), x => Assert.Equal(ConnectionValidity.Invalid, x.Validity));
        Assert.DoesNotContain(collision.LoginMethods, x => x.Key == "shared");
    }

    [Fact]
    public async Task TenThousandConnectionsRemainDeterministicallyOrderedAndDiscoverable()
    {
        var connections = Enumerable.Range(0, 10_000)
            .Select(index => Connection($"connection-{index:D5}", ConnectionScope.HostTenantId, $"provider-{9_999 - index:D5}", index % 13, index == 42))
            .ToArray();
        var source = new StaticSource("configuration", ConnectionSourceOwnership.Configuration, new Dictionary<ConnectionScope, IReadOnlyCollection<IdentityProviderConnection>> { [ConnectionScope.Host] = connections });
        var registry = new DefaultIdentityProviderConnectionRegistry([source], new ConnectionRevisionCalculator());

        var first = await registry.GetAsync(string.Empty);
        var second = await registry.GetAsync(string.Empty);

        Assert.Equal(10_000, first.LoginMethods.Count);
        Assert.Equal(first.LoginMethods.Select(x => x.Id), second.LoginMethods.Select(x => x.Id));
        Assert.Equal(first.LoginMethods.OrderBy(x => x.Order).ThenBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Id), first.LoginMethods.Select(x => x.Id));
        Assert.Equal("connection-00042", Assert.Single(first.LoginMethods, x => x.IsDefault).Id);
        Assert.NotNull(await registry.FindByKeyAsync(string.Empty, "provider-00000"));
        Assert.NotNull(await registry.FindByIdAsync(string.Empty, "connection-09999"));
    }

    private static IdentityProviderConnection Connection(string id, string tenantId, string key, int displayOrder = 0, bool isDefault = false) => new()
    {
        Id = id, TenantId = tenantId, Key = key, AdapterType = "test", AdapterSettingsVersion = 1, AdapterSettings = JsonSerializer.SerializeToElement(new { }),
        DisplayName = key, DisplayOrder = displayOrder, IsDefault = isDefault, IsEnabled = true, MaterialRevision = $"revision-{id}"
    };

    private sealed class StaticSource(string name, ConnectionSourceOwnership ownership, IDictionary<ConnectionScope, IReadOnlyCollection<IdentityProviderConnection>> snapshots) : IIdentityProviderConnectionSource
    {
        public string Name => name;
        public ConnectionSourceOwnership Ownership => ownership;
        public void Add(ConnectionScope scope, IdentityProviderConnection connection) => snapshots[scope] = snapshots.TryGetValue(scope, out var current) ? [.. current, connection] : [connection];
        public ValueTask<ConnectionSourceSnapshot> GetSnapshotAsync(ConnectionScope scope, CancellationToken cancellationToken = default) => ValueTask.FromResult(new ConnectionSourceSnapshot(scope, "v1", snapshots.TryGetValue(scope, out var connections) ? connections : []));
    }
}
