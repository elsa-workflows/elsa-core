using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Services;

/// <summary>Keeps the encrypted authentication ticket (and therefore refresh token) on the server.</summary>
public sealed class ServerExternalAuthenticationTicketStore(IMemoryCache cache) : ITicketStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public Task<string> StoreAsync(AuthenticationTicket ticket) => StoreAsync(ticket, default);

    public Task RenewAsync(string key, AuthenticationTicket ticket) => RenewAsync(key, ticket, default);

    public Task<AuthenticationTicket?> RetrieveAsync(string key) => RetrieveAsync(key, default);

    public Task RemoveAsync(string key) => RemoveAsync(key, default);

    public Task<string> StoreAsync(AuthenticationTicket ticket, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        cache.Set(key, ticket, TicketOptions(ticket));
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        cache.Set(key, ticket, TicketOptions(ticket));
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(cache.TryGetValue(key, out AuthenticationTicket? ticket) ? ticket : null);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }

    private static MemoryCacheEntryOptions TicketOptions(AuthenticationTicket ticket) => new()
    {
        AbsoluteExpiration = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(Lifetime),
        SlidingExpiration = Lifetime
    };
}
