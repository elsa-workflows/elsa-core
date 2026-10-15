using Elsa.Connections.Contracts;
using Elsa.Connections.Models;

namespace Elsa.Connections.Credentials.Workflows.Services;

/// <summary>Fails closed when no durable binding-store provider is configured for the workflow adapter.</summary>
internal sealed class UnavailableConnectionCredentialBindingStore : IConnectionCredentialBindingStore
{
    public Task<ConnectionCredentialBinding?> FindAsync(
        string tenantId,
        string environmentId,
        string logicalBindingId,
        CancellationToken cancellationToken = default) => NotFound(cancellationToken);

    public Task<ConnectionCredentialBinding?> TryCreateAsync(
        string tenantId,
        string environmentId,
        string logicalBindingId,
        string connectionId,
        CancellationToken cancellationToken = default) => NotFound(cancellationToken);

    public Task<ConnectionCredentialBinding?> TryRebindAsync(
        string tenantId,
        string environmentId,
        string logicalBindingId,
        long expectedRevision,
        string connectionId,
        CancellationToken cancellationToken = default) => NotFound(cancellationToken);

    private static Task<ConnectionCredentialBinding?> NotFound(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<ConnectionCredentialBinding?>(cancellationToken)
            : Task.FromResult<ConnectionCredentialBinding?>(null);
}
