namespace Elsa.Services.Models
{
    public interface IActivityBlueprint
    {
        string Id { get; }
        string? Name { get; }
        string? DisplayName { get; }
        string Type { get; }
        bool PersistWorkflow { get; }
        bool LoadWorkflowContext { get; set; }
        bool SaveWorkflowContext { get; set; }
    }
}