namespace Elsa.Telnyx.Models;

[Serializable]
public class TelnyxWebhook
{
    public TelnyxWebhookMeta Meta { get; set; } = null!;
    public TelnyxWebhookData Data { get; set; } = null!;
}