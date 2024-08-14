using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.OrchardCore.Client;

public class DefaultSecurityTokenService(IMemoryCache memoryCache, ISecurityTokenClient securityTokenClient, IOptions<OrchardCoreClientOptions> options) : ISecurityTokenService
{
    private readonly string _cacheKey = $"{nameof(DefaultSecurityTokenService)}:{Guid.NewGuid()}";
    
    public async Task<SecurityToken> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return (await memoryCache.GetOrCreateAsync(_cacheKey, async entry =>
        {
            var securityToken = await RequestSecurityTokenAsync(cancellationToken);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(securityToken.ExpiresIn);
            return securityToken;
        }))!;
    }

    public Task<SecurityToken> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        return RequestSecurityTokenAsync(cancellationToken);
    }
    
    private Task<SecurityToken> RequestSecurityTokenAsync(CancellationToken cancellationToken)
    {
        var clientId = options.Value.ClientId;
        var clientSecret = options.Value.ClientSecret;
        return securityTokenClient.GetSecurityTokenAsync(clientId, clientSecret, cancellationToken);
    }
}