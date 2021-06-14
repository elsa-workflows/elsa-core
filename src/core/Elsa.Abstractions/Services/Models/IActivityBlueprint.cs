using System.Collections.Generic;

namespace Elsa.Services.Models
{
    public interface IActivityBlueprint
    {
        string Id { get; }
        ICompositeActivityBlueprint? Parent { get; }
        string? Name { get; }
        string? DisplayName { get; }
        string? Description { get; }
        string Type { get; }
        bool PersistWorkflow { get; }
        bool LoadWorkflowContext { get; set; }
        bool SaveWorkflowContext { get; set; }
        string? Source { get; set; }
        IDictionary<string, string> PropertyStorageProviders { get; }
    }
}