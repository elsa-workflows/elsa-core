using Elsa.Models;
using Elsa.Persistence.Entities;

namespace Elsa.Runtime.Services;

/// <summary>
/// Manages materialization of <see cref="WorkflowDefinition"/> to <see cref="Workflow"/> objects. 
/// </summary>
public interface IWorkflowDefinitionService
{
    Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
}