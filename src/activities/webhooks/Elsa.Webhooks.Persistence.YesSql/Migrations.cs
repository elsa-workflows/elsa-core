using Elsa.Persistence.YesSql.Data;
using Elsa.Webhooks.Persistence.YesSql.Data;
using Elsa.Webhooks.Persistence.YesSql.Indexes;
using YesSql.Sql;

namespace Elsa.Webhooks.Persistence.YesSql
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<WebhookDefinitionIndex>(
                table => table
                    .Column<string>(nameof(WebhookDefinitionIndex.DefinitionId))
                    .Column<string?>(nameof(WebhookDefinitionIndex.TenantId))
                    .Column<bool>(nameof(WebhookDefinitionIndex.IsEnabled)),
                CollectionNames.WebhookDefinitions);

            return 1;
        }
    }
}
