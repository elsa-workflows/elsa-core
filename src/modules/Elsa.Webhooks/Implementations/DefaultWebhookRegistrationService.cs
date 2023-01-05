using System.Runtime.CompilerServices;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Services;

namespace Elsa.Webhooks.Implementations;

/// <inheritdoc />
public class DefaultWebhookRegistrationService : IWebhookRegistrationService
{
    private readonly IEnumerable<IWebhookRegistrationProvider> _providers;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="providers"></param>
    public DefaultWebhookRegistrationService(IEnumerable<IWebhookRegistrationProvider> providers)
    {
        _providers = providers;
    }
    
    /// <inheritdoc />
    public async ValueTask<IEnumerable<WebhookRegistration>> ListByEventTypeAsync(string eventType, CancellationToken cancellationToken) => 
        await EnumerateByEventTypeAsync(eventType, cancellationToken).ToListAsync(cancellationToken);

    private async IAsyncEnumerable<WebhookRegistration> EnumerateByEventTypeAsync(string eventType, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var provider in _providers)
        {
            var registrations = await provider.ListAsync(eventType, cancellationToken);

            foreach (var registration in registrations)
                yield return registration;
        }
    }
}