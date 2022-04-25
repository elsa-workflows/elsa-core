using Elsa.Models;
using Elsa.Services;

namespace Elsa.Persistence.Entities;

/// <summary>
/// Represents a workflow definition.
/// </summary>
public class WorkflowDefinition : Entity
{
    public string DefinitionId { get; set; } = default!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; } = 1;
    public IActivity Root { get; set; } = default!;
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public IDictionary<string, object> ApplicationProperties { get; set; } = new Dictionary<string, object>();
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }

    public WorkflowDefinition ShallowClone() => (WorkflowDefinition)MemberwiseClone();
}