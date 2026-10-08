using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

public interface IConnectionCredentialProvider
{
    Task<CredentialMaterial> RefreshAsync(string providerId, string accountId, string refreshToken, CancellationToken cancellationToken = default);
}
