using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Creates <see cref="IWorkflowHost"/> objects.
/// </summary>
public interface IWorkflowHostFactory
{
    Task<IWorkflowHost> CreateAsync(
        Workflow workflow, 
        WorkflowState workflowState,
        CancellationToken cancellationToken = default);
    
    Task<IWorkflowHost> CreateAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default);
}