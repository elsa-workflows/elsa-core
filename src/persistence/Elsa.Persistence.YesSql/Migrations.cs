using System.Data;
using Elsa.Data;
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
                    .Column<string>(nameof(WorkflowDefinitionIndex.EntityId))
                    .Column<string>(nameof(WorkflowDefinitionIndex.DefinitionVersionId))
                    .Column<int>(nameof(WorkflowDefinitionIndex.Version))
                    .Column<bool>(nameof(WorkflowDefinitionIndex.IsLatest))
                    .Column<bool>(nameof(WorkflowDefinitionIndex.IsPublished))
                    .Column<bool>(nameof(WorkflowDefinitionIndex.IsEnabled)),
                CollectionNames.WorkflowDefinitions);

            SchemaBuilder.CreateMapIndexTable<WorkflowInstanceIndex>(
                table => table
                    .Column<string?>(nameof(WorkflowInstanceIndex.TenantId))
                    .Column<string>(nameof(WorkflowInstanceIndex.EntityId))
                    .Column<string>(nameof(WorkflowInstanceIndex.DefinitionId))
                    .Column<int>(nameof(WorkflowInstanceIndex.Version))
                    .Column<string?>(nameof(WorkflowInstanceIndex.CorrelationId))
                    .Column<string?>(nameof(WorkflowInstanceIndex.ContextId))
                    .Column(nameof(WorkflowInstanceIndex.WorkflowStatus), DbType.String)
                    .Column(nameof(WorkflowInstanceIndex.CreatedAt), DbType.DateTimeOffset)
                    .Column(nameof(WorkflowInstanceIndex.LastExecutedAt), DbType.DateTimeOffset)
                    .Column(nameof(WorkflowInstanceIndex.FinishedAt), DbType.DateTimeOffset)
                    .Column(nameof(WorkflowInstanceIndex.CancelledAt), DbType.DateTimeOffset)
                    .Column(nameof(WorkflowInstanceIndex.FaultedAt), DbType.DateTimeOffset)
                ,
                CollectionNames.WorkflowInstances);

            SchemaBuilder.CreateMapIndexTable<SuspendedWorkflowIndex>(
                table => table
                    .Column<string>(nameof(SuspendedWorkflowIndex.EntityId))
                    .Column<string?>(nameof(SuspendedWorkflowIndex.TenantId))
                    .Column<string>(nameof(SuspendedWorkflowIndex.InstanceId))
                    .Column<string>(nameof(SuspendedWorkflowIndex.DefinitionId))
                    .Column<int>(nameof(SuspendedWorkflowIndex.Version))
                    .Column<string?>(nameof(SuspendedWorkflowIndex.CorrelationId))
                    .Column<string?>(nameof(SuspendedWorkflowIndex.ContextId))
                    .Column<string>(nameof(SuspendedWorkflowIndex.ActivityId))
                    .Column<string>(nameof(SuspendedWorkflowIndex.ActivityType))
                ,
                CollectionNames.SuspendedWorkflows);

            return 1;
        }
    }
}