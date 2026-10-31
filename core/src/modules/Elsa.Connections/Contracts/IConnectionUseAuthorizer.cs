using System.Security.Claims;

namespace Elsa.Connections.Contracts;

public interface IConnectionUseAuthorizer
{
    Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Scope identifiers select a resource; they are not proof of authorization. Implementations must validate the
/// authenticated principal and apply separate human-use and server-background policies.
/// </summary>
public sealed record ConnectionUseRequest(
    ClaimsPrincipal Principal,
    ConnectionUseKind Kind,
    string TenantId,
    string EnvironmentId,
    string ConnectionId,
    string Purpose);

public enum ConnectionUseKind
{
    Human,
    BackgroundSystem
}
