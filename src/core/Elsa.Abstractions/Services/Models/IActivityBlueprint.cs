namespace Elsa.Services.Models
{
    public interface IActivityBlueprint
    {
        public string Id { get; }
        string? Name { get; }
        public string Type { get; }
        public bool PersistWorkflow { get; }
        bool LoadWorkflowContext { get; set; }
        bool SaveWorkflowContext { get; set; }
    }
}