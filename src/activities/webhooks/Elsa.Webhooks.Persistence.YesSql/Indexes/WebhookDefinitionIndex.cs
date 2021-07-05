using Elsa.Webhooks.Persistence.YesSql.Data;
using Elsa.Webhooks.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Webhooks.Persistence.YesSql.Indexes
{
    public class WebhookDefinitionIndex : MapIndex
    {
        public string DefinitionId { get; set; } = default!;
        public string? TenantId { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class WebhookDefinitionIndexProvider : IndexProvider<WebhookDefinitionDocument>
    {
        public WebhookDefinitionIndexProvider() => CollectionName = CollectionNames.WebhookDefinitions;

        public override void Describe(DescribeContext<WebhookDefinitionDocument> context)
        {
            context.For<WebhookDefinitionIndex>()
                .Map(
                    webhookDefinition => new WebhookDefinitionIndex
                    {
                        DefinitionId = webhookDefinition.DefinitionId,
                        TenantId = webhookDefinition.TenantId,
                        IsEnabled = webhookDefinition.IsEnabled
                    }
                );
        }
    }
}