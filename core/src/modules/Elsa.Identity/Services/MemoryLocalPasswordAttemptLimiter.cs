using System.Security.Cryptography;
using System.Text;
using Elsa.Common;
using Elsa.Identity.Contracts;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.Services;

internal sealed class MemoryLocalPasswordAttemptLimiter(ISystemClock clock, IOptions<LocalPasswordAttemptOptions> options) : ILocalPasswordAttemptLimiter
{
    private readonly object _sync = new();
    private readonly Dictionary<(string TenantId, string UsernameHash), int> _accounts = new();
    private readonly Dictionary<string, int> _sources = new(StringComparer.Ordinal);
    private DateTimeOffset _expiresAt;

    public ValueTask<ILocalPasswordAttempt?> TryBeginAsync(string tenantId, string username, string? source, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var account = (tenantId, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(username))));
        var settings = options.Value;

        lock (_sync)
        {
            var now = clock.UtcNow;
            if (now >= _expiresAt)
            {
                _accounts.Clear();
                _sources.Clear();
                _expiresAt = now.Add(settings.Window);
            }

            if (!HasCapacity(_accounts, account, settings.AccountLimit, settings.PartitionLimit) ||
                source is not null && !HasCapacity(_sources, source, settings.SourceLimit, settings.PartitionLimit))
                return ValueTask.FromResult<ILocalPasswordAttempt?>(null);

            Reserve(_accounts, account);
            if (source is not null)
                Reserve(_sources, source);

            return ValueTask.FromResult<ILocalPasswordAttempt?>(new Attempt(this, account, source, _expiresAt));
        }
    }

    private void Release((string TenantId, string UsernameHash) account, string? source, DateTimeOffset expiresAt)
    {
        lock (_sync)
        {
            if (expiresAt != _expiresAt)
                return;

            Release(_accounts, account);
            if (source is not null)
                Release(_sources, source);
        }
    }

    private static bool HasCapacity<TKey>(Dictionary<TKey, int> counts, TKey key, int limit, int partitionLimit) where TKey : notnull =>
        counts.TryGetValue(key, out var count) ? count < limit : counts.Count < partitionLimit;

    private static void Reserve<TKey>(Dictionary<TKey, int> counts, TKey key) where TKey : notnull =>
        counts[key] = counts.GetValueOrDefault(key) + 1;

    private static void Release<TKey>(Dictionary<TKey, int> counts, TKey key) where TKey : notnull
    {
        if (counts[key] == 1)
            counts.Remove(key);
        else
            counts[key]--;
    }

    private sealed class Attempt(MemoryLocalPasswordAttemptLimiter owner, (string TenantId, string UsernameHash) account, string? source, DateTimeOffset expiresAt) : ILocalPasswordAttempt
    {
        private bool _failed;
        private int _disposed;

        public void MarkFailure() => _failed = true;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0 && !_failed)
                owner.Release(account, source, expiresAt);

            return ValueTask.CompletedTask;
        }
    }
}
