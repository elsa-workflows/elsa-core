using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Secrets.Contracts;

namespace Elsa.Slack.SocketMode.Credentials;

// No workflow or public API can supply a connection, principal or purpose to this facade.
// Its only consumer is the owned listener's fixed-origin URL-opening operation.
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

    internal async Task<ConnectionAccessCredential> ResolveCurrentAsync(CancellationToken cancellationToken)
    {
        await DemandAuthorizedAsync(cancellationToken);
        try
        {
            var reader = new ConnectionCredentialReader(store, secrets, timeProvider, tenantAccessor);
            var current = await reader.ReadAsync(configuration.TenantId, configuration.EnvironmentId, configuration.ConnectionId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (current.Kind != ConnectionCredentialKind.ApiKey)
            {
                throw new ConnectionUnavailableException();
            }
            return current;
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
