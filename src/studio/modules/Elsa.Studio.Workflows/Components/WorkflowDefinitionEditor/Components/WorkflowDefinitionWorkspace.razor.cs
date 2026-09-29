using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// A workspace for editing a workflow definition.
public partial class WorkflowDefinitionWorkspace : IWorkspace
{
    private bool _autoSave;
    private bool _autoApply;
    private int _tabIndex = 0;
    private MudDynamicTabs _dynamicTabs = null!;
    private WorkflowDefinition? _workflowDefinition = null!;
    private WorkflowDefinition? _selectedWorkflowDefinition = null!;
    private WorkflowDefinition? _lastIncomingWorkflowDefinition;
    private WorkflowDefinition? _lastIncomingSelectedWorkflowDefinition;
    private long _workflowDefinitionReloadVersion;
    private WorkflowEditor WorkflowEditor { get; set; } = null!;
    private CodeView CodeView { get; set; } = null!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private IOptions<WorkflowDefinitionOptions> WorkflowDefinitionOptions { get; set; } = null!;

    /// Gets or sets the workflow definition to edit.
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    /// Gets or sets a specific version of the workflow definition to view.
    [Parameter] public WorkflowDefinition SelectedWorkflowDefinition { get; set; } = null!;

    /// <summary>An event that is invoked when a workflow definition has been executed.</summary>
    /// <remarks>The ID of the workflow instance is provided as the value to the event callback.</remarks>
    [Parameter] public Func<string, Task>? WorkflowDefinitionExecuted { get; set; }

    /// Gets or sets the event that occurs when the workflow definition version is updated.
    [Parameter] public Func<WorkflowDefinition, Task>? WorkflowDefinitionVersionSelected { get; set; }

    /// Gets or sets the event that occurs when an activity is selected.
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }

    private bool _isCodeViewTab => _tabIndex == 1;

    /// An event that is invoked when the workflow definition is updated.
    public event Func<Task>? WorkflowDefinitionUpdated;

    /// <inheritdoc />
    public bool IsReadOnly => _selectedWorkflowDefinition.GetIsReadOnly();

    /// <inheritdoc />
    public bool HasWorkflowEditPermission => (_selectedWorkflowDefinition?.Links?.Count(l => l.Rel == "publish") ?? 0) > 0;

    /// Gets the selected activity ID.
    public string? SelectedActivityId => WorkflowEditor?.SelectedActivityId;

    /// Gets the workflow definition serialized as a formatted JSON string.
    public string WorkflowDefinitionSerialized => JsonSerializer.Serialize(WorkflowEditor?.WorkflowDefinition ?? _workflowDefinition, new JsonSerializerOptions { WriteIndented = true });

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _autoSave = WorkflowDefinitionOptions.Value.AutoSaveChangesByDefault;
        _autoApply = WorkflowDefinitionOptions.Value.AutoApplyCodeViewChangesByDefault;
        _lastIncomingWorkflowDefinition = WorkflowDefinition;
        _lastIncomingSelectedWorkflowDefinition = SelectedWorkflowDefinition;
        _workflowDefinition = WorkflowDefinition;
        _selectedWorkflowDefinition = SelectedWorkflowDefinition ?? _workflowDefinition;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var isNewWorkflowDefinition = !ReferenceEquals(_lastIncomingWorkflowDefinition, WorkflowDefinition);
        var isNewSelectedWorkflowDefinition = !ReferenceEquals(_lastIncomingSelectedWorkflowDefinition, SelectedWorkflowDefinition);
        _lastIncomingWorkflowDefinition = WorkflowDefinition;
        _lastIncomingSelectedWorkflowDefinition = SelectedWorkflowDefinition;

        if (!isNewWorkflowDefinition && !isNewSelectedWorkflowDefinition)
            return;

        if (WorkflowEditor != null)
            WorkflowEditor.InvalidateWorkflowDefinitionOperations();

        if (isNewWorkflowDefinition)
            _workflowDefinition = WorkflowDefinition;

        _selectedWorkflowDefinition = SelectedWorkflowDefinition ?? _workflowDefinition;
        _workflowDefinitionReloadVersion++;
    }

    /// Displays the specified workflow definition version.
    public async Task DisplayWorkflowDefinitionVersionAsync(WorkflowDefinition workflowDefinition)
    {
        if (_selectedWorkflowDefinition == workflowDefinition)
            return;

        if (WorkflowEditor != null)
            WorkflowEditor.InvalidateWorkflowDefinitionOperations();

        _workflowDefinitionReloadVersion++;
        _selectedWorkflowDefinition = workflowDefinition;

        if (workflowDefinition.IsLatest)
            _workflowDefinition = workflowDefinition;

        if (WorkflowDefinitionVersionSelected != null)
            await WorkflowDefinitionVersionSelected(_selectedWorkflowDefinition);

        StateHasChanged();
    }

    /// Gets the currently selected workflow definition version.
    public WorkflowDefinition? GetSelectedWorkflowDefinitionVersion() => _selectedWorkflowDefinition;

    /// Determines whether the workspace is currently viewing a specific version of a workflow definition.
    public bool IsSelectedDefinition(string definitionVersionId) => _selectedWorkflowDefinition?.Id == definitionVersionId;

    /// Gets the selected workflow definition.
    public WorkflowDefinition? GetSelectedDefinition() => _selectedWorkflowDefinition;

    /// Displays the latest version of a workflow definition asynchronously.
    public async Task DisplayLatestWorkflowDefinitionVersionAsync()
    {
        var definitionId = _workflowDefinition!.DefinitionId;
        var definition = (await WorkflowDefinitionService.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest))!;
        await DisplayWorkflowDefinitionVersionAsync(definition);
    }

    private async Task OnWorkflowDefinitionPropsUpdated()
    {
        if (WorkflowEditor != null)
            await WorkflowEditor.NotifyWorkflowChangedAsync(_isCodeViewTab);

        if (_isCodeViewTab && CodeView is not null)
            await CodeView.UpdateCodeViewFromEditorAsync(WorkflowDefinitionSerialized);

        if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();
    }

    private async Task OnWorkflowDefinitionUpdated()
    {
        _workflowDefinition = WorkflowEditor.WorkflowDefinition!;
        _selectedWorkflowDefinition = _workflowDefinition;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnWorkflowDefinitionReloaded()
    {
        _workflowDefinitionReloadVersion++;
        await OnWorkflowDefinitionUpdated();
    }

    private async Task OnApplyWorkflowDefinition(WorkflowDefinition deserialized)
    {
        var incomingJson = JsonSerializer.Serialize(deserialized, new JsonSerializerOptions { WriteIndented = true });
        if (string.Equals(WorkflowDefinitionSerialized, incomingJson, StringComparison.Ordinal))
            return;

        if (WorkflowEditor != null)
            await WorkflowEditor.ApplyWorkflowDefinitionAsync(deserialized);
        await OnWorkflowDefinitionUpdated();

        if (WorkflowDefinitionUpdated is not null)
            await WorkflowDefinitionUpdated();
    }
}
