using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Providers;

/// <summary>
/// Projects optional database-owned rows into the effective registry. The backing store is deliberately replaceable.
/// </summary>
public sealed class DatabaseIdentityProviderConnectionSource(
    IIdentityProviderConnectionStore store,
    IConnectionRegistryVersionStore versions,
    IOptionsMonitor<ExternalAuthenticationOptions> options) : IIdentityProviderConnectionSource
{
    public const string SourceName = "database";

    public string Name => SourceName;
    public ConnectionSourceOwnership Ownership => ConnectionSourceOwnership.Database;

    public async ValueTask<ConnectionSourceSnapshot> GetSnapshotAsync(ConnectionScope scope, CancellationToken cancellationToken = default)
    {
        if (!options.CurrentValue.EnableDatabaseConnections)
            return new ConnectionSourceSnapshot(scope, "disabled", []);

        var page = await store.FindAsync(new ConnectionFilter { Scope = scope }, cancellationToken);
        var version = await versions.GetVersionAsync(cancellationToken);
        return new ConnectionSourceSnapshot(scope, version.ToString(System.Globalization.CultureInfo.InvariantCulture), page.Items.ToArray());
    }
}
