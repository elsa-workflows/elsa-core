using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;
using Elsa.Extensions;
using Elsa.Studio.WorkflowContexts.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.WorkflowContexts.Components;

/// <summary>
/// A component that renders the workflow context editor.
/// </summary>
public partial class WorkflowContextsEditor
{
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    /// <summary>
    /// Gets or sets the callback that is invoked when the workflow definition is updated.
    /// </summary>
    [Parameter]
    public EventCallback WorkflowDefinitionUpdated { get; set; }

    [Inject] private IWorkflowContextsProvider WorkflowContextsProvider { get; set; } = null!;

    private ICollection<WorkflowContextProviderDescriptor> WorkflowContextDescriptors { get; set; } = new List<WorkflowContextProviderDescriptor>();
    private ISet<string> SelectedWorkflowContextTypes { get; set; } = new HashSet<string>();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        WorkflowContextDescriptors = (await WorkflowContextsProvider.ListAsync()).ToList();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        SelectedWorkflowContextTypes = WorkflowDefinition.GetWorkflowContextProviderTypes().ToHashSet();
    }

    private async Task OnCheckChanged(WorkflowContextProviderDescriptor descriptor, bool isChecked)
    {
        if (isChecked)
            SelectedWorkflowContextTypes.Add(descriptor.Type);
        else
            SelectedWorkflowContextTypes.Remove(descriptor.Type);

        WorkflowDefinition.SetWorkflowContextProviderTypes(SelectedWorkflowContextTypes);

        if (WorkflowDefinitionUpdated.HasDelegate)
            await WorkflowDefinitionUpdated.InvokeAsync();
    }
}