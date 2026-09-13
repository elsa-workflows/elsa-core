using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.IntegrationTests.Scale;

public sealed class TenantAndRegistryScaleTests
{
    [Test]
    public async Task HostWideConnectionsAreAvailableToEveryTenantAndLegacyTenantConnectionsAreIgnored()
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
            var connection = (await Assert.That(snapshot.Connections).HasSingleItem())!;
            await Assert.That(connection.Connection.TenantId).IsEqualTo(ConnectionScope.HostTenantId);
            await Assert.That(connection.Connection.Key).IsEqualTo("shared");
        }

        source.Add(new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), Connection("collision", "tenant-a", "shared"));
        var snapshotAfterLegacyCollision = await registry.GetAsync("tenant-a");
        var collidedConnection = (await Assert.That(snapshotAfterLegacyCollision.Connections).HasSingleItem())!;
        await Assert.That(collidedConnection.Validity).IsEqualTo(ConnectionValidity.Unknown);
        await Assert.That(snapshotAfterLegacyCollision.LoginMethods).Contains(x => x.Key == "shared");
    }

    [Test]
    public async Task TenThousandConnectionsRemainDeterministicallyOrderedAndDiscoverable()
    {
        var connections = Enumerable.Range(0, 10_000)
            .Select(index => Connection($"connection-{index:D5}", ConnectionScope.HostTenantId, $"provider-{9_999 - index:D5}", index % 13, index == 42))
            .ToArray();
        var source = new StaticSource("configuration", ConnectionSourceOwnership.Configuration, new Dictionary<ConnectionScope, IReadOnlyCollection<IdentityProviderConnection>> { [ConnectionScope.Host] = connections });
        var registry = new DefaultIdentityProviderConnectionRegistry([source], new ConnectionRevisionCalculator());

        var first = await registry.GetAsync(string.Empty);
        var second = await registry.GetAsync(string.Empty);

        await Assert.That(first.LoginMethods.Count).IsEqualTo(10_000);
        await Assert.That(second.LoginMethods.Select(x => x.Id)).IsEquivalentTo(
            first.LoginMethods.Select(x => x.Id),
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(first.LoginMethods.Select(x => x.Id)).IsEquivalentTo(
            first.LoginMethods.OrderBy(x => x.Order).ThenBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Id),
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        var preferredConnection = (await Assert.That(first.LoginMethods).HasSingleItem(x => x.IsPreferred))!;
        await Assert.That(preferredConnection.Id).IsEqualTo("connection-00042");
        await Assert.That(await registry.FindByKeyAsync(string.Empty, "provider-00000")).IsNotNull();
        await Assert.That(await registry.FindByIdAsync(string.Empty, "connection-09999")).IsNotNull();
    }

    private static IdentityProviderConnection Connection(string id, string tenantId, string key, int displayOrder = 0, bool isPreferred = false) => new()
    {
        Id = id, TenantId = tenantId, Key = key, AdapterType = "test", AdapterSettingsVersion = 1, AdapterSettings = JsonSerializer.SerializeToElement(new { }),
        DisplayName = key, DisplayOrder = displayOrder, IsPreferred = isPreferred, IsEnabled = true, MaterialRevision = $"revision-{id}"
    };

    private sealed class StaticSource(string name, ConnectionSourceOwnership ownership, IDictionary<ConnectionScope, IReadOnlyCollection<IdentityProviderConnection>> snapshots) : IIdentityProviderConnectionSource
    {
        public string Name => name;
        public ConnectionSourceOwnership Ownership => ownership;
        public void Add(ConnectionScope scope, IdentityProviderConnection connection) => snapshots[scope] = snapshots.TryGetValue(scope, out var current) ? [.. current, connection] : [connection];
        public ValueTask<ConnectionSourceSnapshot> GetSnapshotAsync(ConnectionScope scope, CancellationToken cancellationToken = default) => ValueTask.FromResult(new ConnectionSourceSnapshot(scope, "v1", snapshots.TryGetValue(scope, out var connections) ? connections : []));
    }
}
