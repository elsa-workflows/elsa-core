using Elsa.Models;

namespace Elsa.Webhooks.Models
{
    public class WebhookDefinition : Entity, ITenantScope
    {
        public string? TenantId { get; set; }
        public string Name { get; set; } = default!;
        public string Path { get; set; } = default!;
        public string? Description { get; set; }
        public string? PayloadTypeName { get; set; }
        public bool IsEnabled { get; set; }
    }
}
