namespace Elsa.Http.Webhooks;

/// <summary>
/// Atomically claims a delivery ID for a webhook source. Multi-replica hosts must share one implementation.
/// </summary>
public interface IWebhookReplayStore
{
    /// <summary>Returns false if the delivery has already been claimed or storage cannot reserve it.</summary>
    ValueTask<bool> TryClaimAsync(string sourceId, string deliveryId, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}
