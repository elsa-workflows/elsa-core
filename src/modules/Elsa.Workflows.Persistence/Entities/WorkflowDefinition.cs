using Elsa.Persistence.Common.Entities;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Persistence.Entities;

/// <summary>
/// Represents a workflow definition.
/// </summary>
public class WorkflowDefinition : VersionedEntity
{
    public string DefinitionId { get; set; } = default!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public IDictionary<string, object> ApplicationProperties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// The name of the workflow materializer to interpret the <see cref="StringData"/> or <see cref="BinaryData"/>.
    /// </summary>
    public string MaterializerName { get; set; } = default!;

    /// <summary>
    /// Provider-specific data.
    /// </summary>
    public string? MaterializerContext { get; set; }
    
    /// <summary>
    /// A textual representation of the workflow. The data is to be interpreted by the configured materializer.
    /// </summary>
    public string? StringData { get; set; }
    
    /// <summary>
    /// A binary representation of the workflow. The data is to be interpreted by the configured materializer.
    /// </summary>
    public byte[]? BinaryData { get; set; }
    
    public WorkflowDefinition ShallowClone() => (WorkflowDefinition)MemberwiseClone();
}