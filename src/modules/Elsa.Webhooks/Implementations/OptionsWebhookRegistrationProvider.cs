using Elsa.Webhooks.Models;
using Elsa.Webhooks.Options;
using Elsa.Webhooks.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Webhooks.Implementations;

/// <summary>
/// Provides webhook registrations from the <see cref="WebhookOptions"/> options.
/// </summary>
public class OptionsWebhookRegistrationProvider : IWebhookRegistrationProvider
{
    private readonly WebhookOptions _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options"></param>
    public OptionsWebhookRegistrationProvider(IOptions<WebhookOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<WebhookRegistration>> ListAsync(string eventType, CancellationToken cancellationToken) => 
        new(_options.Endpoints.Where(x => !x.EventTypes.Any() || x.EventTypes.Contains(eventType)));
}