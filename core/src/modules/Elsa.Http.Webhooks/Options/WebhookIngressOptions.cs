namespace Elsa.Http.Webhooks.Options;

/// <summary>Configures authenticated inbound webhooks.</summary>
public sealed class WebhookIngressOptions
{
    /// <summary>Base64-encoded signing keys indexed by exact configured webhook source ID.</summary>
    public Dictionary<string, string> SigningKeys { get; set; } = new(StringComparer.Ordinal);
}
