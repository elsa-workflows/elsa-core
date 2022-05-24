using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Creates new <see cref="WorkflowInstance"/> objects.
/// </summary>
public interface IWorkflowInstanceFactory
{
    Task<WorkflowInstance> CreateAsync(string workflowDefinitionId, string? correlationId, CancellationToken cancellationToken = default);
    Task<WorkflowInstance> CreateAsync(string workflowDefinitionId, VersionOptions versionOptions, string? correlationId, CancellationToken cancellationToken = default);
    WorkflowInstance Create(Workflow workflow, string? correlationId);
}