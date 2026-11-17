using Elsa.Common;

namespace Elsa.Http.Webhooks.Services;

internal sealed class MemoryWebhookReplayStore(ISystemClock clock) : IWebhookReplayStore
{
    private const int Capacity = 10_000;
    private readonly object _gate = new();
    private readonly HashSet<(string Source, string Delivery)> _claims = new();
    private readonly PriorityQueue<(string Source, string Delivery), DateTimeOffset> _expiry = new();

    public ValueTask<bool> TryClaimAsync(string sourceId, string deliveryId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = clock.UtcNow;

            while (_expiry.TryPeek(out var expired, out var deadline) && deadline <= now)
            {
                _expiry.Dequeue();
                _claims.Remove(expired);
            }

            var delivery = (sourceId, deliveryId);
            if (expiresAt <= now || _claims.Count >= Capacity || !_claims.Add(delivery))
                return ValueTask.FromResult(false);

            _expiry.Enqueue(delivery, expiresAt);
            return ValueTask.FromResult(true);
        }
    }
}
