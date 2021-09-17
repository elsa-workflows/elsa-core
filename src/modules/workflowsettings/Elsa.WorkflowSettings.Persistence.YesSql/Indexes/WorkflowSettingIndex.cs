using Elsa.WorkflowSettings.Persistence.YesSql.Data;
using Elsa.WorkflowSettings.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.WorkflowSettings.Persistence.YesSql.Indexes
{
    public class WorkflowSettingIndex : MapIndex
    {
        public string SettingId { get; set; } = default!;
    }

    public class WorkflowSettingsIndexProvider : IndexProvider<WorkflowSettingDocument>
    {
        public WorkflowSettingsIndexProvider() => CollectionName = CollectionNames.WorkflowSettings;

        public override void Describe(DescribeContext<WorkflowSettingDocument> context)
        {
            context.For<WorkflowSettingIndex>()
                .Map(
                    workflowSettings => new WorkflowSettingIndex
                    {
                        SettingId = workflowSettings.SettingId
                    }
                );
        }
    }
}