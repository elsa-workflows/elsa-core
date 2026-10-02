using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.Alterations.Contracts;
using Elsa.Api.Client.Resources.Alterations.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Alterations.Catalog;
using Elsa.Studio.Alterations.Models;
using Elsa.Studio.Alterations.Services;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Alterations.Components;

/// <summary>
/// The alterations editor shell. Layout = top toolbar / diagram in the middle / bottom inspector
/// panel (Inspect / Variables / Staged tabs). Owns the staging service lifecycle, the selected
/// activity state, the cached variable list, and the Submit / Dry-run / Discard flows.
/// </summary>
public partial class AlterationDesignerHost : IDisposable
{
    [Parameter] public WorkflowInstance? WorkflowInstance { get; set; }
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Always a Flowchart-shaped activity (the page unwraps "Elsa.Workflow" wrappers before
    /// passing it in) so the Flowchart designer provider can claim it.
    /// </summary>
    [Parameter] public JsonObject? FlowchartActivity { get; set; }

    [Inject] private IAlterationStagingService Staging { get; set; } = null!;
    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ILocalizer Localizer { get; set; } = null!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = null!;
    [Inject] private IAlterationCatalog Catalog { get; set; } = null!;

    // Selected activity in the diagram. Drives the Inspect tab and the activity-scoped quick
    // actions. Kept here (not in the bottom panel) because the staging callbacks need the same
    // values when an action button is clicked.
    private string? _selectedActivityId;
    private string? _selectedActivityName;
    private string? _selectedActivityType;

    // Cached variable list — fetched once per instance, refreshed after a ModifyVariable
    // submit so the displayed values stay in sync. Variable values are fetched from the
    // existing /workflow-instances/{id}/variables endpoint via IWorkflowInstanceService.
    private IReadOnlyList<ResolvedVariable>? _variables;
    private bool _variablesLoading;

    private bool _panelCollapsed;
    private bool _busy;
    private AlterationSidePanel? _sidePanel;
    private string? _loadedInstanceId;

    protected override void OnInitialized()
    {
        Staging.Changed += HandleStagingChanged;
        // Per-circuit staging service is shared across pages; clear so opening a fresh editor
        // doesn't show leftovers from a previous instance.
        Staging.Clear();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (WorkflowInstance == null) return;

        if (_loadedInstanceId != null && _loadedInstanceId != WorkflowInstance.Id)
            ResetInstanceState();

        _loadedInstanceId = WorkflowInstance.Id;

        if (_variables == null && !_variablesLoading)
            await LoadVariablesAsync();
    }

    private void HandleStagingChanged() => InvokeAsync(StateHasChanged);

    private void ResetInstanceState()
    {
        Staging.Clear();
        _variables = null;
        _variablesLoading = false;
        _selectedActivityId = null;
        _selectedActivityName = null;
        _selectedActivityType = null;
    }

    private async Task LoadVariablesAsync()
    {
        if (WorkflowInstance == null) return;
        _variablesLoading = true;
        try
        {
            var vars = await WorkflowInstanceService.GetVariablesAsync(WorkflowInstance.Id);
            _variables = vars?.ToList() ?? new List<ResolvedVariable>();
        }
        catch
        {
            _variables = new List<ResolvedVariable>();
        }
        finally
        {
            _variablesLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Diagram click handler. Filters out clicks on the Flowchart / Workflow root so they don't
    /// register as an activity selection — those fire when clicking empty canvas space.
    /// </summary>
    private void OnDiagramActivitySelected(JsonObject activity)
    {
        var typeName = activity.GetTypeName();
        var isContainer = typeName == "Elsa.Flowchart" || typeName == "Elsa.Workflow";
        if (isContainer)
        {
            _selectedActivityId = null;
            _selectedActivityName = null;
            _selectedActivityType = null;
        }
        else
        {
            _selectedActivityId = activity.GetId();
            _selectedActivityName = ResolveDisplayName(activity);
            _selectedActivityType = typeName;
            // Auto-focus the Selection tab so the user immediately sees the activity's actions.
            _sidePanel?.SwitchTo(AlterationSidePanel.Tab.Selection);
        }
        StateHasChanged();
    }

    private static string? ResolveDisplayName(JsonObject activity)
    {
        var name = activity.GetName();
        if (!string.IsNullOrWhiteSpace(name)) return name;
        var displayText = activity.GetDisplayText();
        if (!string.IsNullOrWhiteSpace(displayText)) return displayText;
        return activity.GetId();
    }


    /// <summary>
    /// Stages an alteration triggered by an Inspect-tab button. Activity-scoped descriptors
    /// auto-target the currently selected activity; instance/variable descriptors fall through
    /// to the standard config dialog (which itself populates VariablePicker / VersionPicker
    /// fields automatically).
    /// </summary>
    private Task OnQuickActionAsync(AlterationDescriptor descriptor)
    {
        if (descriptor.Target == AlterationTargetKind.Activity)
        {
            if (string.IsNullOrEmpty(_selectedActivityId))
            {
                Snackbar.Add(Localizer["Select an activity first."], Severity.Warning);
                return Task.CompletedTask;
            }
            return StageAsync(descriptor, _selectedActivityId, _selectedActivityName, existing: null);
        }
        return StageAsync(descriptor, targetActivityId: null, targetDisplayName: null, existing: null);
    }

    /// <summary>
    /// "Modify" button in the Variables tab. Opens the ModifyVariable config dialog with the
    /// variable id pre-filled, so the user only needs to enter the new value.
    /// </summary>
    private async Task OnModifyVariableShortcutAsync(ResolvedVariable variable)
    {
        var modifyVariable = (await Catalog.ListAsync()).FirstOrDefault(d => d.TypeId == "ModifyVariable");
        if (modifyVariable == null) return;

        var prefilled = new Dictionary<string, string>
        {
            ["variableId"] = variable.Id,
        };

        // Open the standard config dialog with the picker already set to this variable. The
        // user edits only the value.
        await StageAsyncWithExistingValues(modifyVariable, targetActivityId: null, targetDisplayName: null, prefilled);
    }

    private Task OnEditStagedAsync(StagedAlteration item)
        => StageAsync(item.Descriptor, item.TargetActivityId, item.TargetActivityDisplayName, item);

    private async Task StageAsync(
        AlterationDescriptor descriptor,
        string? targetActivityId,
        string? targetDisplayName,
        StagedAlteration? existing)
    {
        await StageAsyncWithExistingValues(descriptor, targetActivityId, targetDisplayName, existing?.ConfigValues, existing);
    }

    private async Task StageAsyncWithExistingValues(
        AlterationDescriptor descriptor,
        string? targetActivityId,
        string? targetDisplayName,
        IDictionary<string, string>? prefilledValues,
        StagedAlteration? existing = null)
    {
        IDictionary<string, string>? values = prefilledValues;

        if (descriptor.ConfigFields.Count > 0)
        {
            var parameters = new DialogParameters
            {
                [nameof(AlterationConfigDialog.Descriptor)] = descriptor,
                [nameof(AlterationConfigDialog.TargetLabel)] = targetActivityId != null
                    ? string.Format(Localizer["Targets activity: {0}"], targetDisplayName ?? targetActivityId)
                    : null,
                [nameof(AlterationConfigDialog.ExistingValues)] = values,
                [nameof(AlterationConfigDialog.WorkflowInstanceId)] = WorkflowInstance?.Id,
                [nameof(AlterationConfigDialog.DefinitionId)] = WorkflowDefinition?.DefinitionId,
            };

            var reference = await DialogService.ShowAsync<AlterationConfigDialog>(descriptor.DisplayName, parameters);
            var result = await reference.Result;
            if (result == null || result.Canceled) return;
            values = (IDictionary<string, string>)result.Data!;
        }

        if (existing == null)
        {
            Staging.Add(new StagedAlteration
            {
                Descriptor = descriptor,
                TargetActivityId = targetActivityId,
                TargetActivityDisplayName = targetDisplayName,
                ConfigValues = values is null ? new Dictionary<string, string>() : new Dictionary<string, string>(values),
            });
        }
        else
        {
            existing.ConfigValues.Clear();
            if (values != null)
                foreach (var (k, v) in values)
                    existing.ConfigValues[k] = v;
            Staging.Update(existing);
        }
    }

    private async Task OnDiscardAllAsync()
    {
        var confirm = await DialogService.ShowMessageBoxAsync(
            Localizer["Discard staged alterations?"],
            Localizer["This will clear all staged items. The instance is not affected."],
            yesText: Localizer["Discard"],
            cancelText: Localizer["Keep"]);

        if (confirm == true)
            Staging.Clear();
    }

    private async Task OnDryRunAsync()
    {
        if (WorkflowInstance == null) return;
        _busy = true;
        try
        {
            var api = await BackendApiClientProvider.GetApiAsync<IAlterationsApi>();
            var filter = BuildFilter();
            var response = await api.DryRun(filter, CancellationToken.None);
            var count = response?.WorkflowInstanceIds?.Count() ?? 0;
            Snackbar.Add(string.Format(Localizer["Dry-run: would target {0} instance(s)."], count), Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add(string.Format(Localizer["Dry-run failed: {0}"], ex.Message), Severity.Error);
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Submit goes through the 3-step wizard dialog: review, preview JSON, submit + result.
    /// We don't post directly here — the dialog owns the API call so the user can see and
    /// approve the exact wire payload first.
    /// </summary>
    private async Task OnSubmitAsync()
    {
        if (WorkflowInstance == null || Staging.Items.Count == 0) return;
        _busy = true;
        try
        {
            var parameters = new DialogParameters
            {
                [nameof(AlterationSubmitDialog.WorkflowInstanceId)] = WorkflowInstance.Id,
                [nameof(AlterationSubmitDialog.StagedItems)] = Staging.Items.ToList(),
                [nameof(AlterationSubmitDialog.ViewPlanRequested)] =
                    EventCallback.Factory.Create<string>(this, OnNavigateToPlan),
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = false,
            };

            var reference = await DialogService.ShowAsync<AlterationSubmitDialog>(
                Localizer["Submit alteration plan"], parameters, options);
            var result = await reference.Result;

            if (result?.Canceled == false && result.Data is string planId && !string.IsNullOrEmpty(planId))
            {
                // The dialog already cleared its UI; clear the staging service too so the editor
                // is empty when the user comes back.
                Staging.Clear();
            }
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    private void OnNavigateToPlan(string planId)
    {
        Staging.Clear();
        Navigation.NavigateTo($"alterations/plans/{planId}");
    }

    private AlterationWorkflowInstanceFilter BuildFilter() => new()
    {
        EmptyFilterSelectsAll = false,
        WorkflowInstanceIds = new[] { WorkflowInstance!.Id },
    };

    private Color MapStatusColor() => WorkflowInstance?.Status switch
    {
        WorkflowStatus.Running => Color.Info,
        WorkflowStatus.Finished => Color.Success,
        _ => Color.Default,
    };

    public void Dispose()
    {
        Staging.Changed -= HandleStagingChanged;
    }
}
