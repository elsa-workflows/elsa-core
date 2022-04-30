using Elsa.Models;
using Elsa.Persistence.Entities;

namespace Elsa.Runtime.Services;

/// <summary>
/// Represents a source of workflow definitions.
/// </summary>
public interface IWorkflowDefinitionProvider
{
    string Name { get; }
    ValueTask<IEnumerable<WorkflowDefinitionResult>> GetWorkflowDefinitionsAsync(CancellationToken cancellationToken = default);
}


public record WorkflowDefinitionResult(WorkflowDefinition Definition, Workflow Workflow);