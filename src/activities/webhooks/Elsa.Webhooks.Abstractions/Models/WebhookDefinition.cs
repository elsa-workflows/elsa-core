using Elsa.Models;

namespace Elsa.Webhooks.Abstractions.Models
{
    public class WebhookDefinition : IEntity, ITenantScope
    {
        public string Id { get; set; } = default!;
        public string? TenantId { get; set; }
        public string Name { get; set; } = default!;
        public string Path { get; set; } = default!;
        public string? Description { get; set; }
        public string? PayloadTypeName { get; set; }
        public bool IsEnabled { get; set; }
    }
}
