using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Secrets.Contracts;

namespace Elsa.Slack.SocketMode.Credentials;

// No workflow or public API can supply a connection, principal or purpose to this facade.
// The owned listener resolves the bearer only for opening, then revalidates its lease before effects.
internal sealed class SlackSocketListenerCredentialReader(SlackSocketModeConfiguration configuration,
    IConnectionUseAuthorizer authorizer, IConnectionLifecycleStore store, IManagedSecretManager secrets,
    TimeProvider timeProvider, ITenantAccessor tenantAccessor)
{
    internal const string Purpose = "listen:slack-socket";
    internal const string PrincipalId = "elsa-slack-socket-listener";
    internal const string AuthenticationType = "Elsa.Slack.SocketMode.Listener";

    internal async Task DemandAuthorizedAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, PrincipalId), new Claim("elsa:identity-kind", "system")], AuthenticationType));
            if (!await authorizer.AuthorizeAsync(new ConnectionUseRequest(principal, ConnectionUseKind.BackgroundSystem,
                configuration.TenantId, configuration.EnvironmentId, configuration.ConnectionId, Purpose), cancellationToken))
            {
                throw new ConnectionUnavailableException();
            }
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Socket listener credential access was cancelled.", cancellationToken);
        }
        catch (Exception)
        {
            // Provider/store/host-policy exceptions must not export a secret or connection string.
            throw new ConnectionUnavailableException();
        }
    }

    internal async Task<ConnectionAccessCredential> ResolveCurrentAsync(CancellationToken cancellationToken) =>
        (await ResolveCurrentLeaseAsync(cancellationToken)).Credential;

    internal async Task<SlackSocketCredentialLease> ResolveCurrentLeaseAsync(CancellationToken cancellationToken)
    {
        await DemandAuthorizedAsync(cancellationToken);
        try
        {
            var reader = new ConnectionCredentialReader(store, secrets, timeProvider, tenantAccessor);
            var current = await reader.ReadSnapshotAsync(configuration.TenantId, configuration.EnvironmentId, configuration.ConnectionId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (current.Credential.Kind != ConnectionCredentialKind.ApiKey)
            {
                throw new ConnectionUnavailableException();
            }
            return new(current, configuration.BindingFingerprint);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Socket listener credential access was cancelled.", cancellationToken);
        }
        catch (Exception)
        {
            throw new ConnectionUnavailableException();
        }
    }

    internal async Task DemandCurrentAsync(SlackSocketCredentialLease lease, CancellationToken cancellationToken)
    {
        if (lease == null || lease.BindingFingerprint != configuration.BindingFingerprint)
        {
            throw new ConnectionUnavailableException();
        }
        await DemandAuthorizedAsync(cancellationToken);
        try
        {
            using var tenantContext = tenantAccessor.PushContext(new Tenant { Id = configuration.TenantId, Name = configuration.TenantId });
            var current = await store.FindAsync(configuration.ConnectionId, configuration.TenantId, configuration.EnvironmentId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!ConnectionCredentialReader.CanUseCurrentGeneration(current) ||
                current!.Id != configuration.ConnectionId || current.TenantId != configuration.TenantId ||
                current.EnvironmentId != configuration.EnvironmentId || current.Revision != lease.Revision ||
                current.CurrentGenerationId != lease.GenerationId || current.CurrentSecretName != lease.SecretName)
            {
                throw new ConnectionUnavailableException();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Socket listener credential access was cancelled.", cancellationToken);
        }
        catch (Exception)
        {
            throw new ConnectionUnavailableException();
        }
    }
}
