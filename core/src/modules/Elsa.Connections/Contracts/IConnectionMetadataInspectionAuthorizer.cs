using System.Security.Claims;

namespace Elsa.Connections.Contracts;

/// <summary>
/// The host's separate decision to show nonsecret metadata for one named connection. It is deliberately not a purpose
/// of <see cref="IConnectionUseAuthorizer"/>: a use or management policy must not answer an inspection request it was
/// never written for. The Connections feature registers a deny-all default.
/// </summary>
public interface IConnectionMetadataInspectionAuthorizer
{
    Task<bool> AuthorizeAsync(
        ClaimsPrincipal principal,
        ConnectionMetadataInspectionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Trusted inspection scope: the ambient tenant, the host-configured environment and the named connection.</summary>
public sealed record ConnectionMetadataInspectionRequest(
    string TenantId,
    string EnvironmentId,
    string ConnectionId);
