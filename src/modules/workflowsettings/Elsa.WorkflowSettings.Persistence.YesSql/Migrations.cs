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
            SchemaBuilder.CreateMapIndexTable<WorkflowSettingIndex>(
                table => table
                    .Column<string>(nameof(WorkflowSettingIndex.SettingId)),
                CollectionNames.WorkflowSettings);

            return 1;
        }
    }
}
