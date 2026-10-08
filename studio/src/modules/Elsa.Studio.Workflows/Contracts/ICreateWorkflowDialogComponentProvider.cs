namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// Defines a provider responsible for supplying the type of the component
/// used to display a dialog for creating a new workflow.
/// </summary>
public interface ICreateWorkflowDialogComponentProvider
{
    /// <summary>
    /// Retrieves the type of the component used to display the dialog for creating a new workflow.
    /// </summary>
    /// <returns>
    /// A <see cref="Type"/> representing the component used for the dialog.
    /// </returns>
    Type GetComponentType();
}