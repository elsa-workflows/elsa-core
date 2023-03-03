using Elsa.Mediator.Contracts;
using Elsa.Webhooks.Commands;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Services;

namespace Elsa.Webhooks.Implementations;

/// <summary>
/// Uses a background channel to asynchronously invoke each webhook url.
/// </summary>
public class BackgroundWebhookDispatcher : IWebhookDispatcher
{
    private readonly IBackgroundCommandSender _backgroundCommandSender;
    private readonly IWebhookRegistrationService _webhookRegistrationService;

    /// <summary>
    /// Constructor.
    /// </summary>
    public BackgroundWebhookDispatcher(IBackgroundCommandSender backgroundCommandSender, IWebhookRegistrationService webhookRegistrationService)
    {
        _backgroundCommandSender = backgroundCommandSender;
        _webhookRegistrationService = webhookRegistrationService;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var registrations = await _webhookRegistrationService.ListByEventTypeAsync(webhookEvent.EventType, cancellationToken);

        foreach (var registration in registrations)
        {
            var notification = new InvokeWebhook(registration, webhookEvent);
            await _backgroundCommandSender.SendAsync(notification, cancellationToken);
        }
    }
}