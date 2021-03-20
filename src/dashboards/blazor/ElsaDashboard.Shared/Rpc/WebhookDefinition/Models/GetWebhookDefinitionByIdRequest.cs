using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public class GetWebhookDefinitionByIdRequest
    {
        public GetWebhookDefinitionByIdRequest()
        {
        }

        public GetWebhookDefinitionByIdRequest(string webhookDefinitionId)
        {
            WebhookDefinitionId = webhookDefinitionId;
        }

        [ProtoMember(1)]  public string WebhookDefinitionId { get; set; } = default!;
    }
}