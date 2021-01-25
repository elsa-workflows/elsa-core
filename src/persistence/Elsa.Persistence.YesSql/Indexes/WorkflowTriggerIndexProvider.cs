using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowTriggerIndex : MapIndex
    {
        public string? TenantId { get; set; }
        public string TriggerId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public string WorkflowDefinitionId { get; set; } = default!;
        public string? WorkflowInstanceId { get; set; }
    }
    
    public class WorkflowTriggerIndexProvider : IndexProvider<WorkflowTriggerDocument>
    {
        public WorkflowTriggerIndexProvider() => CollectionName = CollectionNames.WorkflowTriggers;

        public override void Describe(DescribeContext<WorkflowTriggerDocument> context)
        {
            context.For<WorkflowTriggerIndex>()
                .Map(
                    record => new WorkflowTriggerIndex
                    {
                        TenantId = record.TenantId,
                        TriggerId = record.TriggerId,
                        ActivityType = record.ActivityType,
                        WorkflowDefinitionId = record.WorkflowDefinitionId,
                        WorkflowInstanceId = record.WorkflowInstanceId,
                    }
                );
        }
    }
}