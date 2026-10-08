using System.Security.Claims;
using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

/// <summary>Reads an explicitly authorized, nonsecret view of one connection in the trusted host scope.</summary>
public interface IConnectionMetadataInspector
{
    Task<ConnectionInspectionMetadata?> InspectAsync(ClaimsPrincipal principal, string connectionId, CancellationToken cancellationToken = default);
}

public sealed record ConnectionInspectionMetadata(
    string ConnectionId,
    string ProviderId,
    string ProviderAccountId,
    ConnectionStatus Status,
    long Revision);
