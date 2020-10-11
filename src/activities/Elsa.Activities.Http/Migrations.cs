using Elsa.Activities.Http.Indexes;
using Elsa.Data;
using YesSql.Sql;

namespace Elsa.Activities.Http
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<WorkflowInstanceByReceiveHttpRequestIndex>(
                table => table
                    .Column<string>("ActivityId")
                    .Column<string>("RequestPath")
                    .Column<string?>("RequestMethod"),
                CollectionNames.WorkflowInstances);

            return 1;
        }
    }
}