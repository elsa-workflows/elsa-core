using System.Runtime.Serialization;

namespace Elsa.Client.Webhooks.Models
{
    [DataContract]
    public class WebhookDefinition
    {
        [DataMember(Order = 1)] public string Id { get; set; } = default!;

        [DataMember(Order = 2)] public string? TenantId { get; set; }

        [DataMember(Order = 3)] public string Name { get; set; } = default!;

        [DataMember(Order = 4)] public string Path { get; set; } = default!;

        [DataMember(Order = 5)] public string? Description { get; set; }

        [DataMember(Order = 6)] public string? PayloadTypeName { get; set; }
    }
}
