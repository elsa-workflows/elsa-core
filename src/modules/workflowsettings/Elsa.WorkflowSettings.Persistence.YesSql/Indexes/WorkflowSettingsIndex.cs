using Elsa.WorkflowSettings.Persistence.YesSql.Data;
using Elsa.WorkflowSettings.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.WorkflowSettings.Persistence.YesSql.Indexes
{
    public class WorkflowSettingsIndex : MapIndex
    {
        public string DefinitionId { get; set; } = default!;
        //public string? TenantId { get; set; }
        //public bool IsEnabled { get; set; }
    }

    public class WorkflowSettingsIndexProvider : IndexProvider<WorkflowSettingsDocument>
    {
        public WorkflowSettingsIndexProvider() => CollectionName = CollectionNames.WorkflowSettings;

        public override void Describe(DescribeContext<WorkflowSettingsDocument> context)
        {
            context.For<WorkflowSettingsIndex>()
                .Map(
                    workflowSettings => new WorkflowSettingsIndex
                    {
                        DefinitionId = workflowSettings.DefinitionId,
                        //TenantId = webhookDefinition.TenantId,
                        //IsEnabled = webhookDefinition.IsEnabled
                    }
                );
        }
    }
}