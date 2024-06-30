using Elsa.Abstractions;
using Elsa.Webhooks.Stimuli;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using WebhooksCore;

namespace Elsa.Webhooks.Endpoints.Webhooks;

/// An API endpoint that receives webhook events from a webhook source.
[PublicAPI]
internal class Post(IWebhookSourceProvider webhookSourceProvider, IStimulusSender stimulusSender) : ElsaEndpointWithoutRequest
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
        
        var fullTypeName = matchingSource.GetWebhookActivityTypeName(webhookEvent.EventType);
        var stimulus = new WebhookEventReceivedStimulus(webhookEvent.EventType);
        var input = new Dictionary<string, object>
        {
            [nameof(WebhookEvent)] = webhookEvent
        };
        var metadata = new StimulusMetadata
        {
            Input = input
        };
        await stimulusSender.SendAsync(fullTypeName, stimulus, metadata, cancellationToken);
    }
}