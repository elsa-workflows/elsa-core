using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Stores.InMemory;

public sealed class InMemoryExternalAuthenticationStateStore(ISystemClock clock) : IExternalAuthenticationStateStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<(string Purpose, string HandleHash), StateEntry> _entries = new();

    public ValueTask PutAsync<T>(string purpose, string handleHash, T value, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = (purpose, handleHash);

        lock (_syncRoot)
        {
            if (_entries.ContainsKey(key))
                throw new InvalidOperationException("A state entry already exists for the supplied purpose and handle.");

            _entries[key] = new StateEntry(value, expiresAt);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<TakeResult<T>> TryTakeAsync<T>(string purpose, string handleHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_entries.TryGetValue((purpose, handleHash), out var entry) || entry.Value is not T value)
                return ValueTask.FromResult<TakeResult<T>>(new TakeResult<T>.NotFound());

            if (entry.ExpiresAt <= clock.UtcNow)
                return ValueTask.FromResult<TakeResult<T>>(new TakeResult<T>.Expired());

            if (entry.IsConsumed)
                return ValueTask.FromResult<TakeResult<T>>(new TakeResult<T>.AlreadyConsumed());

            entry.IsConsumed = true;
            return ValueTask.FromResult<TakeResult<T>>(new TakeResult<T>.Taken(value));
        }
    }

    private sealed class StateEntry(object? value, DateTimeOffset expiresAt)
    {
        public object? Value { get; } = value;
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
        public bool IsConsumed { get; set; }
    }
}
