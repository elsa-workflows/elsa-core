using Elsa.Models;

namespace Elsa.Persistence.Entities;

/// <summary>
/// Represents a lookup between a label and a workflow definition.
/// </summary>
public class WorkflowDefinitionLabel : Entity
{
    public string WorkflowDefinitionId { get; set; } = default!;
    public string WorkflowDefinitionVersionId { get; set; } = default!;
    public string LabelId { get; set; } = default!;
}