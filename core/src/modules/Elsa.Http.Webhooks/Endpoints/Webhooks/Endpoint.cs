using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Http.Webhooks;
using Elsa.Http.Webhooks.Notifications;
using Elsa.Http.Webhooks.Services;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using WebhooksCore;

namespace Elsa.Http.Webhooks.Endpoints.Webhooks;

/// <summary>
/// Receives authenticated deliveries for configured webhook sources.
/// </summary>
[PublicAPI]
internal class Post(
    IWebhookSourceProvider webhookSourceProvider,
    INotificationSender notificationSender,
    WebhookIngressAuthentication authentication,
    IWebhookReplayStore replayStore) : ElsaEndpointWithoutRequest
{
    private const int MaximumBodyBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public override void Configure()
    {
        Routes("/webhooks");
        Verbs(FastEndpoints.Http.POST);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var request = HttpContext.Request;
        var delivery = authentication.ReadDelivery(request.Headers);
        if (delivery == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        var registeredSources = (await webhookSourceProvider.ListAsync(cancellationToken)).ToList();
        var matchingSources = registeredSources
            .Where(source => string.Equals(source.Id, delivery.SourceId, StringComparison.Ordinal))
            .ToList();
        if (matchingSources.Count != 1 || string.IsNullOrWhiteSpace(matchingSources[0].Name))
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        var body = await ReadBodyAsync(request, cancellationToken);
        if (body == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await HttpContext.Response.StartAsync(cancellationToken);
            return;
        }

        if (!WebhookIngressAuthentication.Authenticate(delivery, body))
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        WebhookEvent? webhookEvent;
        try
        {
            webhookEvent = JsonSerializer.Deserialize<WebhookEvent>(body, JsonOptions);
        }
        catch (JsonException)
        {
            webhookEvent = null;
        }

        if (webhookEvent is null)
        {
            AddError("A valid webhook event body is required.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        var source = matchingSources[0];
        if (string.IsNullOrWhiteSpace(webhookEvent.EventType) ||
            !source.EventTypes.Any(eventType => string.Equals(eventType.EventType, webhookEvent.EventType, StringComparison.Ordinal)) ||
            HasAmbiguousDispatch(registeredSources, source, webhookEvent.EventType))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        if (!await replayStore.TryClaimAsync(delivery.SourceId, delivery.DeliveryId, delivery.ExpiresAt, cancellationToken))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await HttpContext.Response.StartAsync(cancellationToken);
            return;
        }

        await notificationSender.SendAsync(new WebhookEventReceived(webhookEvent, source), cancellationToken);
        await Send.OkAsync(cancellationToken);
    }

    private static bool HasAmbiguousDispatch(IEnumerable<WebhookSource> sources, WebhookSource source, string eventType)
    {
        var activityType = source.GetWebhookActivityTypeName(eventType);
        return sources.Any(other => !ReferenceEquals(other, source) && !string.IsNullOrWhiteSpace(other.Name) &&
            other.EventTypes.Any(otherEvent => !string.IsNullOrWhiteSpace(otherEvent.EventType) &&
                string.Equals(other.GetWebhookActivityTypeName(otherEvent.EventType), activityType, StringComparison.OrdinalIgnoreCase)));
    }

    private static async Task<byte[]?> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength > MaximumBodyBytes)
            return null;

        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (output.Length <= MaximumBodyBytes)
        {
            var count = await request.Body.ReadAsync(buffer, cancellationToken);
            if (count == 0)
                return output.ToArray();

            output.Write(buffer, 0, count);
        }

        return null;
    }
}
