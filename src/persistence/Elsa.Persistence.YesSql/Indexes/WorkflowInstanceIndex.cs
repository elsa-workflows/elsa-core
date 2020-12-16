using System;
using Elsa.Data;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowInstanceIndex : MapIndex
    {
        public string? TenantId { get; set; }
        public string EntityId { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public int Version { get; set; }
        public string? CorrelationId { get; set; }
        public string? ContextId { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastExecutedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public DateTimeOffset? FaultedAt { get; set; }
    }

    public class WorkflowInstanceIndexProvider : IndexProvider<WorkflowInstanceDocument>
    {
        public WorkflowInstanceIndexProvider() => CollectionName = CollectionNames.WorkflowInstances;

        public override void Describe(DescribeContext<WorkflowInstanceDocument> context)
        {
            context.For<WorkflowInstanceIndex>()
                .Map(
                    workflowInstance => new WorkflowInstanceIndex
                    {
                        TenantId = workflowInstance.TenantId,
                        EntityId = workflowInstance.EntityId,
                        DefinitionId = workflowInstance.DefinitionId,
                        Version = workflowInstance.Version,
                        WorkflowStatus = workflowInstance.WorkflowStatus,
                        CorrelationId = workflowInstance.CorrelationId,
                        ContextId = workflowInstance.ContextId,
                        CreatedAt = workflowInstance.CreatedAt.ToDateTimeOffset(),
                        CancelledAt = workflowInstance.CancelledAt?.ToDateTimeOffset(),
                        FinishedAt = workflowInstance.FinishedAt?.ToDateTimeOffset(),
                        LastExecutedAt = workflowInstance.LastExecutedAt?.ToDateTimeOffset(),
                        FaultedAt = workflowInstance.FaultedAt?.ToDateTimeOffset()
                    });
        }
    }
}