using Elsa.Persistence.YesSql.Data;
using Elsa.WorkflowSettings.Persistence.YesSql.Data;
using Elsa.WorkflowSettings.Persistence.YesSql.Indexes;
using YesSql.Sql;

namespace Elsa.WorkflowSettings.Persistence.YesSql
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<WorkflowSettingsIndex>(
                table => table
                    .Column<string>(nameof(WorkflowSettingsIndex.DefinitionId)),
                    //.Column<string?>(nameof(WorkflowSettingsIndex.TenantId))
                    //.Column<bool>(nameof(WorkflowSettingsIndex.IsEnabled)),
                CollectionNames.WorkflowSettings);

            return 1;
        }
    }
}
