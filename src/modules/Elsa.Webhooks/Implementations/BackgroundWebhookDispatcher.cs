using Elsa.Mediator;
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
    private readonly ICommandSender _commandSender;
    private readonly IWebhookRegistrationService _webhookRegistrationService;

    /// <summary>
    /// Constructor.
    /// </summary>
    public BackgroundWebhookDispatcher(ICommandSender commandSender, IWebhookRegistrationService webhookRegistrationService)
    {
        _commandSender = commandSender;
        _webhookRegistrationService = webhookRegistrationService;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var registrations = await _webhookRegistrationService.ListByEventTypeAsync(webhookEvent.EventType, cancellationToken);

        foreach (var registration in registrations)
        {
            var notification = new InvokeWebhook(registration, webhookEvent);
            await _commandSender.SendAsync(notification, CommandStrategy.Background, cancellationToken);
        }
    }
}