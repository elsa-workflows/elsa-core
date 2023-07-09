using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Contracts;

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