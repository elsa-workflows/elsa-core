using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;
using Elsa.Studio.Authorization;
using Elsa.Studio.Extensions;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Contracts;
using Elsa.Studio.Labels.Models;
using Elsa.Studio.Labels.UI.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Components.Outputs;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;
using MudExtensions;

namespace Elsa.Studio.Labels.Components;

/// <summary>
/// A component that renders the workflow context editor.
/// </summary>
public partial class WorkflowDefinitionLabelsEditor
{
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition WorkflowDefinition { get; set; } = default!;

    /// <summary>
    /// Gets or sets the callback that is invoked when the workflow definition is updated.
    /// </summary>
    [Parameter]
    public EventCallback WorkflowDefinitionUpdated { get; set; }

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IWorkflowDefinitionLabelsProvider workflowDefinitionLabelsProvider { get; set; } = default!;

    [CascadingParameter] private UserPermissions Permissions { get; set; } = UserPermissions.Unknown;

    private ICollection<WorkflowDefinitionLabelDescriptor> Labels { get; set; } = new List<WorkflowDefinitionLabelDescriptor>();

    private bool CanUpdate => Permissions.Has(LabelPermissions.WorkflowDefinitionLabels, PermissionVerbs.Update);

    // Adding picks from the label list, which requires labels:view as well.
    private bool CanAdd => CanUpdate && Permissions.Has(LabelPermissions.Labels, PermissionVerbs.View);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Labels = (await workflowDefinitionLabelsProvider.ListAsync(WorkflowDefinition.Id)).ToList();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
    }

    private async Task AddLabelAsync()
    {
        var parameters = new DialogParameters<SelectLabelDialog>
        {
            [nameof(SelectLabelDialog.SelectedLabels)] = Labels.Select(ToLabel).ToHashSet(),
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

        var dialogInstance = await DialogService.ShowAsync<SelectLabelDialog>("Selecteer Label", parameters, options);
        var dialogResult = await dialogInstance.Result;
        if (dialogResult is { Canceled: false, Data: IEnumerable<Label> selectedLabels })
        {
            try
            {
                Labels = (await workflowDefinitionLabelsProvider.UpdateAsync(WorkflowDefinition.Id, selectedLabels.Select(it => it.Id))).ToList();
            }
            catch (Exception e)
            {
                Snackbar.Add(e.ToUserMessage(Localizer), Severity.Error);
            }
        }
    }

    private Label ToLabel(WorkflowDefinitionLabelDescriptor descriptor) => new Label
    {
        Id = descriptor.Id,
        Name = descriptor.Name,
        Color = descriptor.Color,
        NormalizedName = descriptor.Name?.ToUpperInvariant() ?? string.Empty,
    };

    private async Task OnCloseAsync(MudBlazor.MudChip<string> chip)
    {
        var labelToRemove = Labels.FirstOrDefault(x => x.Id == chip.Value);
        if (labelToRemove != null)
        {
            Labels.Remove(labelToRemove);
            Labels = (await workflowDefinitionLabelsProvider.UpdateAsync(WorkflowDefinition.Id, Labels.Select(it => it.Id))).ToList();
        }
    }
}