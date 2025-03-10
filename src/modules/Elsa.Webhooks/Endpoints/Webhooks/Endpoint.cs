using Elsa.Abstractions;
using Elsa.Mediator.Contracts;
using Elsa.Webhooks.Notifications;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using WebhooksCore;

namespace Elsa.Webhooks.Endpoints.Webhooks;

/// <summary>
/// An API endpoint that receives webhook events from a webhook source.
/// </summary>
[PublicAPI]
internal class Post(IWebhookSourceProvider webhookSourceProvider, INotificationSender notificationSender, IStimulusSender stimulusSender) : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/webhooks");
        Verbs(FastEndpoints.Http.POST);
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var webhookEvent = (await HttpContext.Request.ReadFromJsonAsync<WebhookEvent>(cancellationToken))!;
        var webhookSources = (await webhookSourceProvider.ListAsync(cancellationToken)).ToList();
        var matchingSource = webhookSources.FirstOrDefault(source => source.EventTypes.Any(eventType => eventType.EventType == webhookEvent.EventType));

        if (matchingSource == null)
        {
            await SendOkAsync(cancellationToken);
            return;
        }
        
        var notification = new WebhookEventReceived(webhookEvent, matchingSource);
        await notificationSender.SendAsync(notification, cancellationToken);
    }
}