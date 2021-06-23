using System.Runtime.Serialization;

namespace Elsa.Webhooks.Client.Models
{
    public class WebhookDefinitionSummary
    {
        [DataMember(Order = 1)] public string Id { get; set; } = default!;

        [DataMember(Order = 3)] public string? TenantId { get; set; }

        [DataMember(Order = 4)] public string Name { get; set; } = default!;

        [DataMember(Order = 5)] public string Path { get; set; } = default!;

        [DataMember(Order = 6)] public string? Description { get; set; }

        [DataMember(Order = 7)] public string? PayloadTypeName { get; set; }

        [DataMember(Order = 8)] public string? IsEnabled { get; set; }
    }
}