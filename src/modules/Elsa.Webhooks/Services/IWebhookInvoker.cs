using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Services;

/// <summary>
/// Invokes a single registered webhook url.
/// </summary>
public interface IWebhookInvoker
{
    /// <summary>
    /// Invokes the specified webhook registration with the specified webhook event..
    /// </summary>
    Task InvokeWebhookAsync(WebhookRegistration registration, WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}