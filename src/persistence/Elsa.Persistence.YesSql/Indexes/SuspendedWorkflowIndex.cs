using Elsa.Data;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class SuspendedWorkflowIndex : MapIndex
    {
        public string EntityId { get; set; } = default!;
        public string? TenantId { get; set; }
        public string InstanceId { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public int Version { get; set; }
        public string? CorrelationId { get; set; }
        public string? ContextId { get; set; }
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
    }

    public class SuspendedWorkflowIndexProvider : IndexProvider<SuspendedWorkflowBlockingActivityDocument>
    {
        public SuspendedWorkflowIndexProvider() => CollectionName = CollectionNames.SuspendedWorkflows;

        public override void Describe(DescribeContext<SuspendedWorkflowBlockingActivityDocument> context)
        {
            context.For<SuspendedWorkflowIndex>()
                .Map(
                    entity => new SuspendedWorkflowIndex
                    {
                        EntityId = entity.EntityId,
                        TenantId = entity.TenantId,
                        InstanceId = entity.InstanceId,
                        DefinitionId = entity.DefinitionId,
                        Version = entity.Version,
                        CorrelationId = entity.CorrelationId,
                        ContextId = entity.ContextId,
                        ActivityId = entity.ActivityId,
                        ActivityType = entity.ActivityType,
                    });
        }
    }
}