using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Stores.InMemory;

public sealed class InMemoryAuthorizationGrantStore(ISystemClock clock) : IAuthorizationGrantStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, GrantEntry> _entries = new(StringComparer.Ordinal);

    public ValueTask SaveAsync(AuthorizationGrant grant, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (_entries.ContainsKey(grant.CodeHash))
                throw new InvalidOperationException("An authorization grant already exists for the supplied code hash.");

            _entries[grant.CodeHash] = new GrantEntry(Clone(grant));
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<TakeResult<AuthorizationGrant>> TryTakeAsync(string codeHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_entries.TryGetValue(codeHash, out var entry))
                return ValueTask.FromResult<TakeResult<AuthorizationGrant>>(new TakeResult<AuthorizationGrant>.NotFound());

            if (entry.Grant.ExpiresAt <= clock.UtcNow)
                return ValueTask.FromResult<TakeResult<AuthorizationGrant>>(new TakeResult<AuthorizationGrant>.Expired());

            if (entry.IsConsumed)
                return ValueTask.FromResult<TakeResult<AuthorizationGrant>>(new TakeResult<AuthorizationGrant>.AlreadyConsumed());

            entry.IsConsumed = true;
            return ValueTask.FromResult<TakeResult<AuthorizationGrant>>(new TakeResult<AuthorizationGrant>.Taken(Clone(entry.Grant)));
        }
    }

    private static AuthorizationGrant Clone(AuthorizationGrant grant) => new()
    {
        CodeHash = grant.CodeHash,
        ClientId = grant.ClientId,
        CallbackUri = grant.CallbackUri,
        TenantId = grant.TenantId,
        UserId = grant.UserId,
        ExternalSessionId = grant.ExternalSessionId,
        PkceChallenge = grant.PkceChallenge,
        ExpiresAt = grant.ExpiresAt,
        ConsumedAt = grant.ConsumedAt
    };

    private sealed class GrantEntry(AuthorizationGrant grant)
    {
        public AuthorizationGrant Grant { get; } = grant;
        public bool IsConsumed { get; set; }
    }
}
