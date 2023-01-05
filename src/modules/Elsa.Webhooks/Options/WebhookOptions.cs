using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Options;

/// <summary>
/// Provides various options related to webhooks.
/// </summary>
public class WebhookOptions
{
    /// <summary>
    /// Stores a list of webhook registrations.
    /// </summary>
    public ICollection<WebhookRegistration> Endpoints { get; set; } = new List<WebhookRegistration>();
}