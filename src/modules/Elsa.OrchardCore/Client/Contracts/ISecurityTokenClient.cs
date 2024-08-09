namespace Elsa.OrchardCore.Client;

public interface ISecurityTokenClient
{
    Task<SecurityToken> GetSecurityTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);
}