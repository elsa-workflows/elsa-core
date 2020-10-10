using System.Data;
using Elsa.Data;
using Elsa.Indexes;
using YesSql.Sql;

namespace Elsa
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<WorkflowDefinitionIndex>(table => table
                .Column<string>("WorkflowDefinitionId")
                .Column<string>("WorkflowDefinitionVersionId")
                .Column<int>("Version")
                .Column<bool>("IsLatest")
                .Column<bool>("IsPublished")
                .Column<bool>("IsEnabled"));
            
            SchemaBuilder.CreateMapIndexTable<WorkflowInstanceIndex>(table => table
                .Column<string>("WorkflowInstanceId")
                .Column<string>("WorkflowDefinitionId")
                .Column<string?>("CorrelationId")
                .Column("WorkflowStatus", DbType.String)
                .Column("CreatedAt", DbType.DateTimeOffset));
            
            SchemaBuilder.CreateMapIndexTable<WorkflowInstanceBlockingActivitiesIndex>(table => table
                .Column<string>("ActivityId")
                .Column<string>("ActivityType")
                .Column<string?>("CorrelationId")
                .Column("WorkflowStatus", DbType.String)
                .Column("CreatedAt", DbType.DateTimeOffset));
            
            return 1;
        }
    }
}