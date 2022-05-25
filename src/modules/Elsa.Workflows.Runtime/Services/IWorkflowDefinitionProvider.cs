using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Represents a source of workflow definitions.
/// </summary>
public interface IWorkflowDefinitionProvider
{
    string Name { get; }
    ValueTask<IEnumerable<WorkflowDefinitionResult>> GetWorkflowDefinitionsAsync(CancellationToken cancellationToken = default);
}


public record WorkflowDefinitionResult(WorkflowDefinition Definition, Workflow Workflow);