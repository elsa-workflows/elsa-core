using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Secrets.Contracts;
using static Elsa.Connections.Services.ConnectionCredentialEnvelopeCodec;

namespace Elsa.Connections.Services;

// The existing lifecycle service authorizes before calling this reader. The only friend consumer
// is the private Socket listener, which separately authorizes its fixed purpose and principal.
internal sealed class ConnectionCredentialReader(IConnectionLifecycleStore store, IManagedSecretManager secrets,
    TimeProvider timeProvider, ITenantAccessor tenantAccessor)
{
    internal async Task<ConnectionAccessCredential> ReadAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken)
    {
        using var tenantContext = tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });
        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (!CanUseCurrentGeneration(connection))
        {
            throw new ConnectionUnavailableException();
        }
        ConnectionCredentialEnvelope? material;
        try
        {
            var payload = await secrets.ResolveGenerationAsync(connection!.CurrentSecretName!, connection.Id, connection.CurrentGenerationId!, cancellationToken);
            material = Deserialize(payload.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Connection access was cancelled.", cancellationToken);
        }
        catch (Exception)
        {
            throw new ConnectionUnavailableException();
        }
        if (!IsValidEnvelope(material) ||
            (material!.Kind ?? ConnectionCredentialKind.OAuth) == ConnectionCredentialKind.OAuth &&
            material.AccessTokenExpiresAt <= timeProvider.GetUtcNow())
        {
            throw new ConnectionUnavailableException();
        }
        var latest = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (latest == null || latest.Status != ConnectionStatus.Active ||
            latest.Revision != connection!.Revision || latest.CurrentGenerationId != connection.CurrentGenerationId ||
            latest.CurrentSecretName != connection.CurrentSecretName ||
            latest.OperationStatus is not (CredentialOperationStatus.None or CredentialOperationStatus.Completed))
        {
            throw new ConnectionUnavailableException();
        }
        return material!.Kind == ConnectionCredentialKind.ApiKey
            ? new ConnectionAccessCredential(ConnectionCredentialKind.ApiKey, material.AccessToken!, null)
            : new ConnectionAccessCredential(ConnectionCredentialKind.OAuth, material.AccessToken!, material.AccessTokenExpiresAt);
    }

    internal static bool CanUseCurrentGeneration(IntegrationConnection? connection) =>
        connection is { Status: ConnectionStatus.Active, OperationStatus: CredentialOperationStatus.None or CredentialOperationStatus.Completed } &&
        !string.IsNullOrWhiteSpace(connection.CurrentSecretName) && !string.IsNullOrWhiteSpace(connection.CurrentGenerationId);
}
