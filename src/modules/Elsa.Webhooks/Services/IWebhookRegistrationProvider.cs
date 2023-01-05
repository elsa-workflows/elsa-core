using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Services;

/// <summary>
/// Provides a list of webhook registrations.
/// </summary>
public interface IWebhookRegistrationProvider
{
    /// <summary>
    /// Returns a list of webhook registrations.
    /// </summary>
    ValueTask<IEnumerable<WebhookRegistration>> ListAsync(string eventType, CancellationToken cancellationToken);
}