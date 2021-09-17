using Elsa.Persistence.YesSql.Documents;

namespace Elsa.WorkflowSettings.Persistence.YesSql.Documents
{
    public class WorkflowSettingDocument : YesSqlDocument
    {
        public string SettingId { get; set; } = default!;
        public string WorkflowBlueprintId { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string Value { get; set; } = default!;
    }
}