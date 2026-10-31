namespace Elsa.Studio.Http.Webhooks;

/// <summary>Backend permission resources guarding the webhook management UI.</summary>
public static class WebhookPermissions
{
    /// <summary>Webhook administration in Studio. The inbound <c>POST /webhooks</c> event sink stays anonymous.</summary>
    public const string Webhooks = "http/webhooks";
}
