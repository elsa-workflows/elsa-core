using System.Data;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Indexes;
using YesSql.Sql;

namespace Elsa.Persistence.YesSql
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<WorkflowDefinitionIndex>(
                table => table
                    .Column<string?>(nameof(WorkflowDefinitionIndex.TenantId))
                    .Column<string>(nameof(WorkflowDefinitionIndex.DefinitionId))
                    .Column<string>(nameof(WorkflowDefinitionIndex.DefinitionVersionId))
                    .Column<int>(nameof(WorkflowDefinitionIndex.Version))
                    .Column<bool>(nameof(WorkflowDefinitionIndex.IsLatest))
                    .Column<bool>(nameof(WorkflowDefinitionIndex.IsPublished))
                    .Column<bool>(nameof(WorkflowDefinitionIndex.IsEnabled)),
                CollectionNames.WorkflowDefinitions);

            SchemaBuilder.CreateMapIndexTable<WorkflowInstanceIndex>(
                table => table
                    .Column<string?>(nameof(WorkflowInstanceIndex.TenantId))
                    .Column<string>(nameof(WorkflowInstanceIndex.InstanceId))
                    .Column<string>(nameof(WorkflowInstanceIndex.DefinitionId))
                    .Column<int>(nameof(WorkflowInstanceIndex.Version))
                    .Column<string?>(nameof(WorkflowInstanceIndex.CorrelationId))
                    .Column<string?>(nameof(WorkflowInstanceIndex.ContextId))
                    .Column<string?>(nameof(WorkflowInstanceIndex.ContextType))
                    .Column<string?>(nameof(WorkflowInstanceIndex.Name))
                    .Column(nameof(WorkflowInstanceIndex.WorkflowStatus), DbType.String)
                    .Column(nameof(WorkflowInstanceIndex.CreatedAt), DbType.DateTime)
                    .Column(nameof(WorkflowInstanceIndex.LastExecutedAt), DbType.DateTime)
                    .Column(nameof(WorkflowInstanceIndex.FinishedAt), DbType.DateTime)
                    .Column(nameof(WorkflowInstanceIndex.CancelledAt), DbType.DateTime)
                    .Column(nameof(WorkflowInstanceIndex.FaultedAt), DbType.DateTime)
                ,
                CollectionNames.WorkflowInstances);
            
            SchemaBuilder.CreateMapIndexTable<WorkflowInstanceBlockingActivitiesIndex>(
                table => table
                    .Column<string?>(nameof(WorkflowInstanceBlockingActivitiesIndex.TenantId))
                    .Column<string>(nameof(WorkflowInstanceBlockingActivitiesIndex.ActivityId))
                    .Column<string>(nameof(WorkflowInstanceBlockingActivitiesIndex.ActivityType))
                    .Column<string?>(nameof(WorkflowInstanceBlockingActivitiesIndex.CorrelationId))
                    .Column(nameof(WorkflowInstanceBlockingActivitiesIndex.WorkflowStatus), DbType.String)
                    .Column(nameof(WorkflowInstanceBlockingActivitiesIndex.CreatedAt), DbType.DateTime),
                CollectionNames.WorkflowInstances);
            
            SchemaBuilder.CreateMapIndexTable<WorkflowExecutionLogRecordIndex>(
                table => table
                    .Column<string?>(nameof(WorkflowExecutionLogRecordIndex.RecordId))
                    .Column<string?>(nameof(WorkflowExecutionLogRecordIndex.TenantId))
                    .Column<string>(nameof(WorkflowExecutionLogRecordIndex.WorkflowInstanceId))
                    .Column(nameof(WorkflowExecutionLogRecordIndex.Timestamp), DbType.DateTime),
                CollectionNames.WorkflowExecutionLog);
            
            SchemaBuilder.CreateMapIndexTable<BookmarkIndex>(
                table => table
                    .Column<string?>(nameof(BookmarkIndex.BookmarkId))
                    .Column<string?>(nameof(BookmarkIndex.TenantId))
                    .Column<string>(nameof(BookmarkIndex.Hash))
                    .Column<string>(nameof(BookmarkIndex.ActivityType))
                    .Column<string>(nameof(BookmarkIndex.WorkflowInstanceId)),
                CollectionNames.Bookmarks);

            return 1;
        }
    }
}