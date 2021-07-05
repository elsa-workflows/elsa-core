using Elsa.Persistence.YesSql.Documents;

namespace Elsa.Webhooks.Persistence.YesSql.Documents
{
    public class WebhookDefinitionDocument : YesSqlDocument
    {
        public string DefinitionId { get; set; } = default!;
        public string? TenantId { get; set; }
        public string Name { get; set; } = default!;
        public string Path { get; set; } = default!;
        public string? Description { get; set; }
        public string? PayloadTypeName { get; set; }
        public bool IsEnabled { get; set; }
    }
}