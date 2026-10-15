using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Stores.InMemory;

public sealed class InMemoryConnectionObservationStore : IConnectionObservationStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, ConnectionObservation> _observations = new(StringComparer.Ordinal);

    public ValueTask<ConnectionObservation?> FindLatestAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
            return ValueTask.FromResult(_observations.GetValueOrDefault(connectionId));
    }

    public ValueTask SaveLatestAsync(ConnectionObservation observation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (!_observations.TryGetValue(observation.ConnectionId, out var existing) || observation.ObservedAt >= existing.ObservedAt)
                _observations[observation.ConnectionId] = observation;
        }

        return ValueTask.CompletedTask;
    }
}
