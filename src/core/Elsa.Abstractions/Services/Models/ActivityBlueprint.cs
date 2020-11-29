namespace Elsa.Services.Models
{
    public class ActivityBlueprint : IActivityBlueprint
    {
        public ActivityBlueprint()
        {
        }

        public ActivityBlueprint(
            string id,
            string? name,
            string type,
            bool persistWorkflow,
            bool loadWorkflowContext,
            bool saveWorkflowContext)
        {
            Id = id;
            Name = name;
            Type = type;
            PersistWorkflow = persistWorkflow;
            LoadWorkflowContext = loadWorkflowContext;
            SaveWorkflowContext = saveWorkflowContext;
        }

        public string Id { get; set; } = default!;
        public string? Name { get; set; }
        public string Type { get; set; } = default!;
        public bool PersistWorkflow { get; set; }
        public bool LoadWorkflowContext { get; set; }
        public bool SaveWorkflowContext { get; set; }
    }
}