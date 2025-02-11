namespace Elsa.OrchardCore.Client;

public interface ISecurityTokenService
{
    Task<SecurityToken> GetTokenAsync(CancellationToken cancellationToken = default);
    Task<SecurityToken> RefreshTokenAsync(CancellationToken cancellationToken = default);
}