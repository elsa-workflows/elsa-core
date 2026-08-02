using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Stores.InMemory;

public sealed class InMemoryPreviewResultStore(ISystemClock clock) : IPreviewResultStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, PreviewEntry> _entries = new(StringComparer.Ordinal);

    public ValueTask SaveAsync(PreviewResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (_entries.ContainsKey(result.HandleHash))
                throw new InvalidOperationException("A preview result already exists for the supplied handle.");

            _entries[result.HandleHash] = new PreviewEntry(result);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<TakeResult<PreviewResult>> TryTakeAsync(string handleHash, string administratorId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_entries.TryGetValue(handleHash, out var entry) || !string.Equals(entry.Result.AdministratorId, administratorId, StringComparison.Ordinal))
                return ValueTask.FromResult<TakeResult<PreviewResult>>(new TakeResult<PreviewResult>.NotFound());

            if (entry.Result.ExpiresAt <= clock.UtcNow)
                return ValueTask.FromResult<TakeResult<PreviewResult>>(new TakeResult<PreviewResult>.Expired());

            if (entry.IsConsumed)
                return ValueTask.FromResult<TakeResult<PreviewResult>>(new TakeResult<PreviewResult>.AlreadyConsumed());

            entry.IsConsumed = true;
            return ValueTask.FromResult<TakeResult<PreviewResult>>(new TakeResult<PreviewResult>.Taken(entry.Result));
        }
    }

    private sealed class PreviewEntry(PreviewResult result)
    {
        public PreviewResult Result { get; } = result;
        public bool IsConsumed { get; set; }
    }
}
