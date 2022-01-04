using System;
using System.Collections.Generic;
using Elsa.Contracts;

namespace Elsa.Persistence.Entities;

/// <summary>
/// Represents a workflow definition.
/// </summary>
public class WorkflowDefinition : Entity
{
    public string DefinitionId { get; set; } = default!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Version { get; set; } = 1;
    public IActivity Root { get; set; } = default!;
    public ICollection<ITrigger> Triggers { get; set; } = new List<ITrigger>();
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }

    public WorkflowDefinition ShallowClone() => (WorkflowDefinition)MemberwiseClone();
}