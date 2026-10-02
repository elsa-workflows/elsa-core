using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

/// <summary>
/// Provides the default implementation for the <see cref="ICreateWorkflowDialogComponentProvider"/> interface.
/// This implementation returns the type of the component used to display the dialog for creating a new workflow.
/// </summary>
public class DefaultCreateWorkflowDialogComponentProvider : ICreateWorkflowDialogComponentProvider
{
    /// <inheritdoc />
    public Type GetComponentType() => typeof(CreateWorkflowDialog);
}