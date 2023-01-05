using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Services;

/// <summary>
/// Provides a list of webhook registrations.
/// </summary>
public interface IWebhookRegistrationService
{
    /// <summary>
    /// Returns a list of webhook registrations matching the specified event.
    /// </summary>
    ValueTask<IEnumerable<WebhookRegistration>> ListByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);
}