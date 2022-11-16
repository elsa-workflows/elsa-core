namespace Elsa.Telnyx.Models;

[Serializable]
public class TelnyxWebhook
{
    public TelnyxWebhookMeta Meta { get; set; } = default!;
    public TelnyxWebhookData Data { get; set; } = default!;
}