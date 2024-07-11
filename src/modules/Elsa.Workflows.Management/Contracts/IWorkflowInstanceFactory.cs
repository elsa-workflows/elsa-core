using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Creates new <see cref="WorkflowInstance"/> objects.
/// </summary>
public interface IWorkflowInstanceFactory
{
    /// <summary>
    /// Creates a new <see cref="WorkflowState"/> object.
    /// </summary>
    WorkflowState CreateWorkflowState(Workflow workflow, WorkflowInstanceOptions? options = null);

    /// <summary>
    /// Creates a new <see cref="WorkflowInstance"/> object.
    /// </summary>
    WorkflowInstance CreateWorkflowInstance(Workflow workflow, WorkflowInstanceOptions? options = null);
}