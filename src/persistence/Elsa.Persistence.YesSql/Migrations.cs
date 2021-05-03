using System;
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
                    .Column<string>(nameof(WorkflowDefinitionIndex.Tag)),
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
                    .Column<int>(nameof(WorkflowInstanceIndex.WorkflowStatus))
                    .Column<DateTime>(nameof(WorkflowInstanceIndex.CreatedAt))
                    .Column<DateTime>(nameof(WorkflowInstanceIndex.LastExecutedAt))
                    .Column<DateTime>(nameof(WorkflowInstanceIndex.FinishedAt))
                    .Column<DateTime>(nameof(WorkflowInstanceIndex.CancelledAt))
                    .Column<DateTime>(nameof(WorkflowInstanceIndex.FaultedAt))
                ,
                CollectionNames.WorkflowInstances);

            SchemaBuilder.CreateMapIndexTable<WorkflowInstanceBlockingActivitiesIndex>(
                table => table
                    .Column<string?>(nameof(WorkflowInstanceBlockingActivitiesIndex.TenantId))
                    .Column<string>(nameof(WorkflowInstanceBlockingActivitiesIndex.ActivityId))
                    .Column<string>(nameof(WorkflowInstanceBlockingActivitiesIndex.ActivityType))
                    .Column<string?>(nameof(WorkflowInstanceBlockingActivitiesIndex.CorrelationId))
                    .Column<string>(nameof(WorkflowInstanceBlockingActivitiesIndex.WorkflowStatus))
                    .Column<DateTime>(nameof(WorkflowInstanceBlockingActivitiesIndex.CreatedAt)),
                CollectionNames.WorkflowInstances);

            SchemaBuilder.CreateMapIndexTable<WorkflowExecutionLogRecordIndex>(
                table => table
                    .Column<string?>(nameof(WorkflowExecutionLogRecordIndex.RecordId))
                    .Column<string?>(nameof(WorkflowExecutionLogRecordIndex.TenantId))
                    .Column<string>(nameof(WorkflowExecutionLogRecordIndex.WorkflowInstanceId))
                    .Column<string>(nameof(WorkflowExecutionLogRecordIndex.ActivityId))
                    .Column<string>(nameof(WorkflowExecutionLogRecordIndex.ActivityType))
                    .Column<string>(nameof(WorkflowExecutionLogRecordIndex.EventName))
                    .Column<DateTime>(nameof(WorkflowExecutionLogRecordIndex.Timestamp)),
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