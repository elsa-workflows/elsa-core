using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes;

public class TriggerIndex : MapIndex
{
    public string? TenantId { get; set; }
    public string TriggerId { get; set; } = default!;
    public string Hash { get; set; } = default!;
    public string ModelType { get; set; } = default!;
    public string ActivityType { get; set; } = default!;
    public string WorkflowDefinitionId { get; set; } = default!;
}

public class TriggerIndexProvider : IndexProvider<TriggerDocument>
{
    public TriggerIndexProvider() => CollectionName = CollectionNames.Triggers;

    public override void Describe(DescribeContext<TriggerDocument> context)
    {
        context.For<TriggerIndex>()
            .Map(
                record => new TriggerIndex()
                {
                    TenantId = record.TenantId,
                    TriggerId = record.TriggerId,
                    Hash = record.Hash,
                    ModelType = record.ModelType,
                    ActivityType = record.ActivityType,
                    WorkflowDefinitionId = record.WorkflowDefinitionId,
                }
            );
    }
}