using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

/// <summary>
/// Host-only credential access for trusted background execution. Do not expose this contract through HTTP or
/// accept caller-selected identity kinds; the implementation mints its own system identity and authorizes scope.
/// </summary>
public interface IConnectionBackgroundUseService
{
    Task<ConnectionAccessCredential> ResolveForUseAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);
}
