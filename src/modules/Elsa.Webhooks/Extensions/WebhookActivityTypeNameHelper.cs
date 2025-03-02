using Humanizer;
using WebhooksCore;

namespace Elsa.Webhooks;

public static class WebhookActivityTypeNameHelper
{
    public static string GetWebhookActivityTypeName(this WebhookSource webhookSource, string eventType)
    {
        var ns = GetWebhookActivityNamespace(webhookSource);
        return $"{ns}.{eventType.Dehumanize()}";
    }

    public static string GetWebhookActivityNamespace(this WebhookSource webhookSource)
    {
        var webhookSourceName = webhookSource.Name.Dehumanize();
        return $"Webhooks.{webhookSourceName}";
    }
}