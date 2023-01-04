using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Services;

/// <summary>
/// Asynchronously invokes all registered webhooks.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>
    /// Dispatches the specified webhook event.
    /// </summary>
    Task DispatchAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}