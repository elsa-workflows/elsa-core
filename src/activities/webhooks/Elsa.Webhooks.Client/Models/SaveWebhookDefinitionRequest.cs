using System.Runtime.Serialization;

namespace Elsa.Webhooks.Client.Models
{
    [DataContract]
    public sealed class SaveWebhookDefinitionRequest
    {
        [DataMember(Order = 1)] public string? WebhookDefinitionId { get; set; }
        [DataMember(Order = 2)] public string? TenantId { get; set; }
        [DataMember(Order = 3)] public string? Name { get; set; }
        [DataMember(Order = 4)] public string? Path { get; set; }
        [DataMember(Order = 5)] public string? Description { get; set; }
        [DataMember(Order = 5)] public string? PayloadTypeName { get; set; }        
        [DataMember(Order = 7)] public bool IsEnabled { get; set; }
    }
}
