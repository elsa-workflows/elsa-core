using Elsa.Persistence.Common.Entities;
using Elsa.Workflows.Core.Models;

namespace Elsa.ActivityDefinitions.Entities;

public class ActivityDefinition : VersionedEntity
{
    public string DefinitionId { get; set; } = default!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public IDictionary<string, object> ApplicationProperties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A textual representation of the workflow. The data is to be interpreted by the configured materializer.
    /// </summary>
    public string? Data { get; set; }
    
    public ActivityDefinition ShallowClone() => (ActivityDefinition)MemberwiseClone();
}