namespace Elsa.Services.Models
{
    public class ActivityBlueprint : IActivityBlueprint
    {
        public ActivityBlueprint()
        {
        }

        public ActivityBlueprint(
            string id,
            ICompositeActivityBlueprint? parent,
            string? name,
            string? displayName,
            string? description,
            string type,
            bool persistWorkflow,
            bool loadWorkflowContext,
            bool saveWorkflowContext,
            bool persistOutput,
            string? source)
        {
            Id = id;
            Parent = parent;
            Name = name;
            DisplayName = displayName;
            Description = description;
            Type = type;
            PersistWorkflow = persistWorkflow;
            LoadWorkflowContext = loadWorkflowContext;
            SaveWorkflowContext = saveWorkflowContext;
            Source = source;
        }

        public string Id { get; set; } = default!;
        public ICompositeActivityBlueprint? Parent { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; } = default!;
        public bool PersistWorkflow { get; set; }
        public bool LoadWorkflowContext { get; set; }
        public bool SaveWorkflowContext { get; set; }
        public string? Source { get; set; }
        public bool PersistOutput { get; set; }

        public override string ToString() => Type;
    }
}