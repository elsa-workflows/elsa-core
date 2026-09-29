using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Designer;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Refit;
using static Elsa.Studio.Workflows.Designer.StateMachineDesignerConstants;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines;

/// <summary>
/// Coordinates State Machine semantics, X6 visuals, and Blazor inspectors.
/// </summary>
public partial class StateMachineDesignerWrapper
{
    private StateMachineEditorSession? _session;
    private StateMachineCanvasGraph? _canvasGraph;
    private StateMachineCanvas? _canvas;
    private IDictionary<string, ActivityStats>? _activityStats;
    private JsonObject? _loadedParameterStateMachine;
    private IDictionary<string, ActivityStats>? _loadedParameterActivityStats;
    private string? _selectedStateId;
    private string? _selectedTransitionId;
    private string? _selectedActivityId;
    private string? _selectedSlotName;
    private string? _pendingDeleteStateId;
    private string _newStateName = "";
    private string _newTransitionName = "";
    private string? _newTransitionFromId;
    private string? _newTransitionToId;
    private bool _showOutline;
    private bool _processingCanvasChange;
    private IReadOnlyCollection<string> _knownExpressionProviderTypes = ["JavaScript"];
    private readonly List<string> _recentActivityTypes = [];

    [Parameter] public JsonObject StateMachine { get; set; } = [];
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }
    [Parameter] public EventCallback<JsonObject> ActivityDoubleClick { get; set; }
    [Parameter] public EventCallback GraphUpdated { get; set; }

    [CascadingParameter] private DragDropManager DragDropManager { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;
    [Inject] private IActivityNameGenerator ActivityNameGenerator { get; set; } = null!;
    [Inject] private IStateMachineMapper StateMachineMapper { get; set; } = null!;
    [Inject] private StateMachineValidator StateMachineValidator { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = null!;
    [Inject] private IExpressionService ExpressionService { get; set; } = null!;

    private StateMachineStateNode? SelectedState =>
        _session != null && _selectedStateId != null ? TryGetState(_selectedStateId) : null;

    private StateMachineTransitionEdge? SelectedTransition =>
        _session != null && _selectedTransitionId != null ? TryGetTransition(_selectedTransitionId) : null;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _knownExpressionProviderTypes = BooleanConditionEditorDialog
                .FilterProviders(await ExpressionService.ListDescriptorsAsync())
                .Select(x => x.Type)
                .ToArray();
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or ApiException)
        {
            // The editor dialog exposes the recoverable provider-load warning. Keep the
            // inspector's safe built-in fallback until providers can be loaded there.
        }
    }

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(StateMachine, _loadedParameterStateMachine) && ReferenceEquals(ActivityStats, _loadedParameterActivityStats))
            return;

        // The parent echoes a freshly exported JsonObject after GraphUpdated. Treat an
        // equivalent activity as an acknowledgement so session-only canvas geometry and
        // the current selection survive that render cycle.
        if (_session?.CanExport == true && JsonNode.DeepEquals(StateMachine, _session.Export()))
        {
            TrackParameterState(StateMachine, ActivityStats);
            _activityStats = ActivityStats;
            return;
        }

        TrackParameterState(StateMachine, ActivityStats);
        LoadSession(StateMachine, ActivityStats);
    }

    public Task LoadStateMachineAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStats = null)
    {
        TrackParameterState(activity, activityStats);
        LoadSession(activity, activityStats);
        return InvokeAsync(StateHasChanged);
    }

    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot update activity because the designer is read-only.");

        if (string.Equals(StateMachine.GetId(), id, StringComparison.Ordinal))
        {
            await LoadStateMachineAsync(activity, _activityStats);
            if (GraphUpdated.HasDelegate)
                await GraphUpdated.InvokeAsync();
            return;
        }

        if (TryUpdateSlotActivity(id, activity))
            await ApplySessionChangesAsync();
    }

    public Task UpdateActivityStatsAsync(string id, ActivityStats stats)
    {
        _activityStats ??= new Dictionary<string, ActivityStats>();
        _activityStats[id] = stats;
        ActivityStats = _activityStats;
        return Task.CompletedTask;
    }

    public async Task SelectActivityAsync(string id)
    {
        if (_session == null)
            return;

        if (string.Equals(StateMachine.GetId(), id, StringComparison.Ordinal))
        {
            await SelectCanvasAsync();
            return;
        }

        if (TryFindSlotActivity(id, out var activity, out var state, out var transition, out var slotName))
        {
            _selectedStateId = state == null ? null : _session.GetStateVisualId(state);
            _selectedTransitionId = transition == null ? null : _session.GetTransitionVisualId(transition);
            _selectedSlotName = slotName;
            await SelectSlotActivityForPropertiesAsync(activity);
            return;
        }

        var stateMatch = _session.Graph.States.FirstOrDefault(x => string.Equals(x.Name, id, StringComparison.Ordinal));
        if (stateMatch != null)
            await SelectStateAsync(stateMatch);
    }

    public Task<JsonObject> ReadRootActivityAsync()
    {
        if (_session == null)
            return Task.FromResult(StateMachine);

        try
        {
            return Task.FromResult(_session.Export());
        }
        catch (InvalidOperationException ex)
        {
            throw new DiagramDesignerValidationException(ex.Message);
        }
    }

    public Task ZoomToFitAsync() => _canvas?.ZoomToFitAsync() ?? Task.CompletedTask;
    public Task CenterContentAsync() => _canvas?.CenterContentAsync() ?? Task.CompletedTask;

    private void LoadSession(JsonObject activity, IDictionary<string, ActivityStats>? activityStats)
    {
        StateMachine = activity;
        ActivityStats = activityStats;
        _activityStats = activityStats;
        _session = new(StateMachineMapper, StateMachineValidator, activity);
        _canvasGraph = _session.ProjectCanvas();
        _selectedStateId = null;
        _selectedTransitionId = null;
        _pendingDeleteStateId = null;
        SetNewTransitionDefaults();
    }

    private async Task ApplySessionChangesAsync()
    {
        if (_session == null)
            return;

        _canvasGraph = _session.ProjectCanvas();
        SetNewTransitionDefaults();

        if (_session.CanExport)
        {
            if (GraphUpdated.HasDelegate)
                await GraphUpdated.InvokeAsync();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task AddStateAsync()
    {
        if (IsReadOnly || _session == null)
            return;

        var name = StateMachineDesignerNames.GetUniqueStateName(_session.Graph, _newStateName);
        _selectedStateId = _session.AddState(name);
        _selectedTransitionId = null;
        _newStateName = "";
        await ApplySessionChangesAsync();
    }

    private async Task AddTransitionAsync()
    {
        if (IsReadOnly || _session == null || _newTransitionFromId == null || _newTransitionToId == null)
            return;

        var name = StateMachineDesignerNames.GetUniqueTransitionName(_session.Graph, _newTransitionName);
        _selectedTransitionId = _session.AddTransition(_newTransitionFromId, _newTransitionToId, name, name);
        _selectedStateId = null;
        _newTransitionName = "";
        await ApplySessionChangesAsync();
    }

    private Task SetInitialStateAsync(ChangeEventArgs args) => MutateAsync(() => _session!.SetInitialState(NormalizeOptional(args.Value)));
    private Task SetCurrentStateAsync(ChangeEventArgs args) => MutateAsync(() => _session!.SetCurrentState(NormalizeOptional(args.Value)));

    private async Task RenameSelectedStateAsync(string name)
    {
        if (_session == null || _selectedStateId == null || IsReadOnly)
            return;

        var unique = StateMachineDesignerNames.GetUniqueStateName(_session.Graph, name, SelectedState);
        _session.RenameState(_selectedStateId, unique);
        await ApplySessionChangesAsync();
    }

    private Task SetTransitionNameAsync(string? value) =>
        MutateAsync(() => _session!.SetTransitionName(_selectedTransitionId!, StateMachineDesignerNames.GetUniqueTransitionName(_session!.Graph, value, SelectedTransition, true)));

    private Task SetTransitionDisplayNameAsync(string? value) => MutateAsync(() => _session!.SetTransitionDisplayName(_selectedTransitionId!, value));

    private Task SetTransitionFromAsync(string stateName) => SetSelectedTransitionEndpointAsync(stateName, true);
    private Task SetTransitionToAsync(string stateName) => SetSelectedTransitionEndpointAsync(stateName, false);

    private async Task SetSelectedTransitionEndpointAsync(string stateName, bool source)
    {
        if (_session == null || _selectedTransitionId == null || SelectedTransition == null || IsReadOnly)
            return;

        var state = _session.Graph.States.FirstOrDefault(x => string.Equals(x.Name, stateName, StringComparison.Ordinal));
        if (state == null)
            return;

        var stateId = _session.GetStateVisualId(state);
        if (source)
            _session.SetTransitionSource(_selectedTransitionId, stateId);
        else
            _session.SetTransitionTarget(_selectedTransitionId, stateId);
        await ApplySessionChangesAsync();
    }

    private async Task SetStateSlotAsync(StateMachineSlotValueChange change)
    {
        if (_session == null || _selectedStateId == null || IsReadOnly)
            return;

        _session.SetStateSlot(_selectedStateId, ParseStateSlot(change.SlotName), ParseJsonSlot(change.Value, change.SlotName));
        await ApplySessionChangesAndRefreshSlotAsync(change.SlotName, true);
    }

    private async Task SetTransitionSlotAsync(StateMachineSlotValueChange change)
    {
        if (_session == null || _selectedTransitionId == null || IsReadOnly)
            return;

        _session.SetTransitionSlot(_selectedTransitionId, ParseTransitionSlot(change.SlotName), ParseJsonSlot(change.Value, change.SlotName));
        await ApplySessionChangesAndRefreshSlotAsync(change.SlotName, true);
    }

    private async Task ClearStateSlotAsync(string slotName)
    {
        if (_session == null || _selectedStateId == null || IsReadOnly || !IsStateActivitySlot(slotName))
            return;
        _session.SetStateSlot(_selectedStateId, ParseStateSlot(slotName), null);
        await ApplySessionChangesAndRefreshSlotAsync(slotName, false);
    }

    private async Task ClearTransitionSlotAsync(string slotName)
    {
        if (_session == null || _selectedTransitionId == null || IsReadOnly || !IsTransitionActivitySlot(slotName))
            return;
        _session.SetTransitionSlot(_selectedTransitionId, ParseTransitionSlot(slotName), null);
        await ApplySessionChangesAndRefreshSlotAsync(slotName, false);
    }

    private Task AddStateActivityAsync(string slotName) => OpenActivityPickerAsync(slotName, false, true);

    private Task ReplaceStateActivityAsync(string slotName) => OpenActivityPickerAsync(slotName, true, true);

    private Task SelectStateActivityAsync(string slotName)
    {
        if (_session == null || _selectedStateId == null || !IsStateActivitySlot(slotName) || SelectedState is not { } state)
            return Task.CompletedTask;

        return SelectActivityForPropertiesAsync(GetStateSlot(state, slotName), slotName);
    }

    private async Task OpenStateActivityAsync(string slotName)
    {
        if (_session == null || _selectedStateId == null || !IsStateActivitySlot(slotName) || SelectedState is not { } state)
            return;

        if (GetStateSlot(state, slotName) is not JsonObject activity || !activity.IsActivity())
            return;

        await OpenSlotActivityAsync(activity);
    }

    private Task AddTransitionActivityAsync(string slotName) => OpenActivityPickerAsync(slotName, false, false);

    private Task ReplaceTransitionActivityAsync(string slotName) => OpenActivityPickerAsync(slotName, true, false);

    private Task SelectTransitionActivityAsync(string slotName)
    {
        if (_session == null || _selectedTransitionId == null || !IsTransitionActivitySlot(slotName) || SelectedTransition is not { } transition)
            return Task.CompletedTask;

        return SelectActivityForPropertiesAsync(GetTransitionSlot(transition, slotName), slotName);
    }

    private async Task SelectActivityForPropertiesAsync(JsonNode? slot, string slotName)
    {
        if (slot is not JsonObject activity || !activity.IsActivity())
            return;

        _selectedSlotName = slotName;
        await SelectSlotActivityForPropertiesAsync(activity);
    }

    private async Task OpenTransitionActivityAsync(string slotName)
    {
        if (_session == null || _selectedTransitionId == null || !IsTransitionActivitySlot(slotName) || SelectedTransition is not { } transition)
            return;

        if (GetTransitionSlot(transition, slotName) is not JsonObject activity || !activity.IsActivity())
            return;

        await OpenSlotActivityAsync(activity);
    }

    private async Task OpenSlotActivityAsync(JsonObject activity)
    {
        await SelectSlotActivityForPropertiesAsync(activity);

        // Let the host decide whether the activity owns a nested designer. This supports
        // Sequence as well as any other current or future composite activity.
        if (ActivityDoubleClick.HasDelegate)
            await ActivityDoubleClick.InvokeAsync(activity);
    }

    private Task OpenStateActivityJsonAsync(string slotName)
    {
        if (_session == null || _selectedStateId == null || !IsStateActivitySlot(slotName) || SelectedState is not { } state)
            return Task.CompletedTask;

        var stateId = _selectedStateId;
        return OpenActivityJsonEditorAsync(slotName, GetStateSlot(state, slotName), value => ApplyEditedStateActivityJsonAsync(stateId, slotName, value));
    }

    private Task OpenTransitionActivityJsonAsync(string slotName)
    {
        if (_session == null || _selectedTransitionId == null || !IsTransitionActivitySlot(slotName) || SelectedTransition is not { } transition)
            return Task.CompletedTask;

        var transitionId = _selectedTransitionId;
        return OpenActivityJsonEditorAsync(slotName, GetTransitionSlot(transition, slotName), value => ApplyEditedTransitionActivityJsonAsync(transitionId, slotName, value));
    }

    private async Task OpenActivityJsonEditorAsync(string slotName, JsonNode? slot, Func<string, Task> applyAsync)
    {
        if (slot == null)
            return;

        var originalActivity = slot is JsonObject obj && obj.IsActivity() ? obj : null;
        var label = IsReadOnly ? Localizer["View {0} activity JSON", slotName] : Localizer["Edit {0} activity JSON", slotName];
        var helperText = GetActivityJsonHelperText(originalActivity);
        var parameters = new DialogParameters<CodeEditorDialog>
        {
            { x => x.Label, label },
            { x => x.HelperText, helperText },
            { x => x.Value, StateMachinePresentationFormatter.JsonSlot(slot) },
            { x => x.LanguageLabel, Localizer["JSON"] },
            { x => x.MonacoLanguage, "json" },
            { x => x.RequireExplicitApply, true },
            { x => x.IsReadOnly, IsReadOnly },
            { x => x.Validator, (Func<string, string?>)(value => ValidateActivityJson(value, originalActivity)) }
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large,
            Position = DialogPosition.Center
        };
        var dialog = await DialogService.ShowAsync<CodeEditorDialog>(label, parameters, options);
        var result = await dialog.Result;

        if (IsReadOnly || result is not { Canceled: false, Data: string value })
            return;

        await applyAsync(value);
    }

    private string GetActivityJsonHelperText(JsonObject? originalActivity)
    {
        if (IsReadOnly)
            return Localizer["Review the complete activity definition."];

        return originalActivity == null
            ? Localizer["Repair the activity definition. Include a type, id, and nodeId."]
            : Localizer["Edit the complete activity definition. The activity id and nodeId cannot be changed here."];
    }

    private async Task ApplyEditedStateActivityJsonAsync(string stateId, string slotName, string value)
    {
        if (IsReadOnly || _session == null || TryGetState(stateId) == null)
            return;

        _selectedStateId = stateId;
        _session.SetStateSlot(stateId, ParseStateSlot(slotName), JsonNode.Parse(value));
        await ApplySessionChangesAndRefreshSlotAsync(slotName, true);
    }

    private async Task ApplyEditedTransitionActivityJsonAsync(string transitionId, string slotName, string value)
    {
        if (IsReadOnly || _session == null || TryGetTransition(transitionId) == null)
            return;

        _selectedTransitionId = transitionId;
        _session.SetTransitionSlot(transitionId, ParseTransitionSlot(slotName), JsonNode.Parse(value));
        await ApplySessionChangesAndRefreshSlotAsync(slotName, true);
    }

    private string? ValidateActivityJson(string value, JsonObject? originalActivity)
    {
        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(value);
        }
        catch (JsonException exception)
        {
            return Localizer[
                "Invalid JSON at line {0}, column {1}. Check the highlighted syntax and try again.",
                (exception.LineNumber ?? 0) + 1,
                (exception.BytePositionInLine ?? 0) + 1];
        }

        if (parsed is not JsonObject activity)
            return Localizer["The activity definition must be a JSON object."];
        if (!activity.IsActivity())
            return Localizer["The activity definition must include a type."];
        if (string.IsNullOrWhiteSpace(activity.GetId()) || string.IsNullOrWhiteSpace(activity.GetNodeId()))
            return Localizer["The activity definition must include an id and nodeId."];

        if (originalActivity != null &&
            (!string.Equals(activity.GetId(), originalActivity.GetId(), StringComparison.Ordinal) ||
             !string.Equals(activity.GetNodeId(), originalActivity.GetNodeId(), StringComparison.Ordinal)))
            return Localizer["The activity id and nodeId cannot be changed here."];

        return null;
    }

    private async Task OpenActivityPickerAsync(string slotName, bool replacing, bool isStateSlot)
    {
        if (IsReadOnly || _session == null ||
            (isStateSlot && (_selectedStateId == null || !IsStateActivitySlot(slotName))) ||
            (!isStateSlot && (_selectedTransitionId == null || !IsTransitionActivitySlot(slotName))))
            return;

        var title = GetActivityPickerTitle(slotName, replacing);
        var parameters = new DialogParameters<StateMachineActivityPickerDialog>
        {
            { x => x.SlotName, slotName },
            { x => x.IsReplacing, replacing },
            { x => x.RecentActivityTypes, _recentActivityTypes.ToArray() }
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large
        };
        var dialog = await DialogService.ShowAsync<StateMachineActivityPickerDialog>(title, parameters, options);
        var result = await dialog.Result;

        // Do not touch the slot until the picker returns an explicit descriptor. In particular,
        // cancelling Replace leaves the exact existing JSON object in place.
        if (result is not { Canceled: false, Data: ActivityDescriptor descriptor })
            return;

        RememberRecentActivity(descriptor.TypeName);
        if (isStateSlot)
            await ApplyStateActivityDescriptorAsync(slotName, descriptor);
        else
            await ApplyTransitionActivityDescriptorAsync(slotName, descriptor);
    }

    private async Task OpenConditionEditorAsync(string slotName)
    {
        if (IsReadOnly || _session == null || _selectedTransitionId == null || !string.Equals(slotName, "condition", StringComparison.Ordinal))
            return;

        var transitionId = _selectedTransitionId;
        var transition = TryGetTransition(transitionId);
        if (transition == null)
            return;

        var parameters = new DialogParameters<BooleanConditionEditorDialog>
        {
            { x => x.Condition, transition.Condition },
            { x => x.IsReadOnly, IsReadOnly }
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large,
            Position = DialogPosition.Center
        };
        var dialog = await DialogService.ShowAsync<BooleanConditionEditorDialog>(Localizer["Edit transition condition"], parameters, options);
        var result = await dialog.Result;

        // The condition editor owns a local draft. A cancelled dialog, including Escape,
        // must not enter the session mutation path. Applying Always uses null to clear the
        // condition slot, so the result DTO also distinguishes that from cancellation.
        if (result is not { Canceled: false, Data: BooleanConditionDialogResult { Applied: true } edit }
            || _session == null
            || TryGetTransition(transitionId) is not { } currentTransition
            || JsonNode.DeepEquals(currentTransition.Condition, edit.Condition))
            return;

        _session.SetTransitionSlot(transitionId, StateMachineTransitionSlot.Condition, edit.Condition);
        await ApplySessionChangesAsync();
    }

    private async Task OnStateSlotDropAsync(string slotName)
    {
        if (IsReadOnly || _session == null || _selectedStateId == null || !IsStateActivitySlot(slotName) || DragDropManager.Payload is not ActivityDescriptor descriptor)
            return;

        await ApplyStateActivityDescriptorAsync(slotName, descriptor, clearDragPayload: true);
    }

    private async Task ApplyStateActivityDescriptorAsync(string slotName, ActivityDescriptor descriptor, bool clearDragPayload = false)
    {
        if (IsReadOnly || _session == null || _selectedStateId == null || !IsStateActivitySlot(slotName))
            return;

        _session.SetStateSlot(_selectedStateId, ParseStateSlot(slotName), CreateSlotActivity(descriptor));
        if (clearDragPayload)
            DragDropManager.Payload = null;
        await ApplySessionChangesAndRefreshSlotAsync(slotName, true);
    }

    private async Task OnTransitionSlotDropAsync(string slotName)
    {
        if (IsReadOnly || _session == null || _selectedTransitionId == null || !IsTransitionActivitySlot(slotName) || DragDropManager.Payload is not ActivityDescriptor descriptor)
            return;

        await ApplyTransitionActivityDescriptorAsync(slotName, descriptor, clearDragPayload: true);
    }

    private async Task ApplyTransitionActivityDescriptorAsync(string slotName, ActivityDescriptor descriptor, bool clearDragPayload = false)
    {
        if (IsReadOnly || _session == null || _selectedTransitionId == null || !IsTransitionActivitySlot(slotName))
            return;

        var activity = CreateSlotActivity(descriptor);
        _session.SetTransitionSlot(_selectedTransitionId, ParseTransitionSlot(slotName), activity);
        if (clearDragPayload)
            DragDropManager.Payload = null;
        await ApplySessionChangesAndRefreshSlotAsync(slotName, true);
    }

    private static bool IsTransitionActivitySlot(string slotName) =>
        string.Equals(slotName, "trigger", StringComparison.Ordinal) ||
        string.Equals(slotName, "action", StringComparison.Ordinal);

    private static bool IsStateActivitySlot(string slotName) =>
        string.Equals(slotName, "entry", StringComparison.Ordinal) ||
        string.Equals(slotName, "exit", StringComparison.Ordinal);

    private bool HasDiagramDesigner(JsonNode? slot) => slot is JsonObject activity &&
        activity.IsActivity() && DiagramDesignerService.HasDiagramDesigner(activity);

    private void RememberRecentActivity(string activityType)
    {
        _recentActivityTypes.RemoveAll(x => string.Equals(x, activityType, StringComparison.Ordinal));
        _recentActivityTypes.Insert(0, activityType);
        if (_recentActivityTypes.Count > 8)
            _recentActivityTypes.RemoveRange(8, _recentActivityTypes.Count - 8);
    }

    private string GetActivityPickerTitle(string slotName, bool replacing)
    {
        if (replacing)
            return Localizer[$"Choose a replacement {slotName} activity"];

        var article = string.Equals(slotName, "trigger", StringComparison.OrdinalIgnoreCase) ? "a" : "an";
        return Localizer[$"Choose {article} {slotName} activity"];
    }

    private async Task ApplySessionChangesAndRefreshSlotAsync(string slotName, bool selectSlot)
    {
        await ApplySessionChangesAsync();
        var slot = SelectedState != null ? GetStateSlot(SelectedState, slotName) : SelectedTransition != null ? GetTransitionSlot(SelectedTransition, slotName) : null;
        if (selectSlot && slot is JsonObject activity && activity.IsActivity())
        {
            _selectedSlotName = slotName;
            await SelectSlotActivityForPropertiesAsync(activity);
        }
        else
        {
            _selectedSlotName = null;
            await SelectRootActivityForPropertiesAsync();
        }
    }

    private async Task SelectStateByVisualIdAsync(string visualId)
    {
        if (_session == null || TryGetState(visualId) == null)
            return;
        _selectedStateId = visualId;
        _selectedTransitionId = null;
        _pendingDeleteStateId = null;
        await SelectRootActivityForPropertiesAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectTransitionByVisualIdAsync(string visualId)
    {
        if (_session == null || TryGetTransition(visualId) == null)
            return;
        _selectedTransitionId = visualId;
        _selectedStateId = null;
        _pendingDeleteStateId = null;
        await SelectRootActivityForPropertiesAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectStateAsync(StateMachineStateNode state)
    {
        if (_session == null) return;
        await SelectStateByVisualIdAsync(_session.GetStateVisualId(state));
        if (!_showOutline && _canvas != null) await _canvas.SelectCellAsync(_selectedStateId!, true);
    }

    private async Task SelectTransitionAsync(StateMachineTransitionEdge transition)
    {
        if (_session == null) return;
        await SelectTransitionByVisualIdAsync(_session.GetTransitionVisualId(transition));
        if (!_showOutline && _canvas != null) await _canvas.SelectCellAsync(_selectedTransitionId!, false);
    }

    private async Task SelectCanvasAsync()
    {
        _selectedStateId = null;
        _selectedTransitionId = null;
        _pendingDeleteStateId = null;
        await SelectRootActivityForPropertiesAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ReconcileCanvasAsync(JsonElement graphJson)
    {
        if (_session == null || IsReadOnly || _processingCanvasChange || !graphJson.TryGetProperty("cells", out var cells))
            return;

        _processingCanvasChange = true;
        try
        {
            var seenTransitions = new HashSet<string>(StringComparer.Ordinal);
            var semanticChanged = false;

            foreach (var cell in cells.EnumerateArray())
            {
                var shape = GetString(cell, "shape");
                var id = GetString(cell, "id");
                if (id == null) continue;

                if (shape == "elsa-state-machine-state" && TryGetState(id) != null && cell.TryGetProperty("position", out var position))
                {
                    _session.SetStatePosition(id, GetDouble(position, "x"), GetDouble(position, "y"));
                    continue;
                }

                if (shape != "elsa-state-machine-transition") continue;
                if (!TryGetEndpointCell(cell, "source", out var sourceId) || !TryGetEndpointCell(cell, "target", out var targetId)) continue;

                var existing = TryGetTransition(id);
                if (existing == null)
                {
                    if (IsSyntheticStateId(sourceId) || IsSyntheticStateId(targetId))
                        continue;

                    var name = StateMachineDesignerNames.GetUniqueTransitionName(_session.Graph);
                    _selectedTransitionId = _session.AddTransition(sourceId, targetId, name, name);
                    seenTransitions.Add(_selectedTransitionId);
                    _selectedStateId = null;
                    semanticChanged = true;
                    continue;
                }

                seenTransitions.Add(id);
                var projected = _session.ProjectCanvas().Transitions.First(x => x.VisualId == id);
                if (!IsSyntheticStateId(sourceId) && !string.Equals(projected.SourceStateVisualId, sourceId, StringComparison.Ordinal))
                {
                    _session.SetTransitionSource(id, sourceId);
                    semanticChanged = true;
                }
                if (!IsSyntheticStateId(targetId) && !string.Equals(projected.TargetStateVisualId, targetId, StringComparison.Ordinal))
                {
                    _session.SetTransitionTarget(id, targetId);
                    semanticChanged = true;
                }

                _session.SetTransitionVertices(id, ReadVertices(cell));
            }

            var removedTransitionIds = _session.ProjectCanvas().Transitions
                .Select(x => x.VisualId)
                .Where(id => !seenTransitions.Contains(id))
                .ToList();
            foreach (var transitionId in removedTransitionIds)
            {
                _session.DeleteTransition(transitionId);
                if (_selectedTransitionId == transitionId) _selectedTransitionId = null;
                semanticChanged = true;
            }

            if (semanticChanged)
                await ApplySessionChangesAsync();
        }
        finally
        {
            _processingCanvasChange = false;
        }
    }

    private Task HandleDeleteRequestAsync(StateMachineDeleteRequest request) =>
        request.Kind == "state" ? RequestDeleteStateAsync(request.VisualId) : DeleteTransitionAsync(request.VisualId);

    private Task RequestDeleteSelectedStateAsync() => _selectedStateId == null ? Task.CompletedTask : RequestDeleteStateAsync(_selectedStateId);

    private Task ViewSelectedStateTransitionsAsync()
    {
        _showOutline = true;
        return InvokeAsync(StateHasChanged);
    }

    private async Task RequestDeleteStateAsync(string visualId)
    {
        if (IsReadOnly || _session == null || TryGetState(visualId) == null) return;
        if (GetConnectedTransitionCount(visualId) == 0)
        {
            _session.DeleteState(visualId);
            _selectedStateId = null;
            await ApplySessionChangesAsync();
            return;
        }

        _pendingDeleteStateId = visualId;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ConfirmDeleteStateAsync()
    {
        if (_session == null || _pendingDeleteStateId == null) return;
        _session.DeleteState(_pendingDeleteStateId);
        _pendingDeleteStateId = null;
        _selectedStateId = null;
        _selectedTransitionId = null;
        await ApplySessionChangesAsync();
    }

    private void CancelDeleteState() => _pendingDeleteStateId = null;

    private Task DeleteSelectedTransitionAsync() => _selectedTransitionId == null ? Task.CompletedTask : DeleteTransitionAsync(_selectedTransitionId);

    private async Task DeleteTransitionAsync(string visualId)
    {
        if (IsReadOnly || _session == null || TryGetTransition(visualId) == null) return;
        _session.DeleteTransition(visualId);
        if (_selectedTransitionId == visualId) _selectedTransitionId = null;
        await ApplySessionChangesAsync();
    }

    private Task MutateAsync(Action mutation)
    {
        if (IsReadOnly || _session == null) return Task.CompletedTask;
        mutation();
        return ApplySessionChangesAsync();
    }

    private async Task AutoLayoutAsync()
    {
        if (_canvas != null) await _canvas.AutoLayoutAsync();
    }

    private void ShowDiagram() => _showOutline = false;
    private void ShowOutline() => _showOutline = true;

    private async Task SelectIssueAsync(StateMachineValidationIssue issue)
    {
        if (_session == null || string.IsNullOrWhiteSpace(issue.Target)) return;
        var state = _session.Graph.States.FirstOrDefault(x => IsIssueForTarget(issue, GetStateIssueTarget(x)));
        if (state != null)
        {
            await SelectStateAsync(state);
            return;
        }

        var transition = _session.Graph.Transitions.FirstOrDefault(x => IsIssueForTarget(issue, GetTransitionIssueTarget(x)));
        if (transition != null)
            await SelectTransitionAsync(transition);
    }

    private void SetNewTransitionDefaults()
    {
        if (_canvasGraph == null || _canvasGraph.States.Count == 0)
        {
            _newTransitionFromId = null;
            _newTransitionToId = null;
            return;
        }

        var ids = _canvasGraph.States.Select(x => x.VisualId).ToHashSet(StringComparer.Ordinal);
        if (_newTransitionFromId == null || !ids.Contains(_newTransitionFromId)) _newTransitionFromId = _canvasGraph.States[0].VisualId;
        if (_newTransitionToId == null || !ids.Contains(_newTransitionToId)) _newTransitionToId = _canvasGraph.States.ElementAtOrDefault(1)?.VisualId ?? _newTransitionFromId;
    }

    private string? GetStateVisualIdByName(string? name)
    {
        if (_session == null || string.IsNullOrWhiteSpace(name)) return null;
        var matches = _session.Graph.States.Where(x => string.Equals(x.Name, name, StringComparison.Ordinal)).ToList();
        return matches.Count == 1 ? _session.GetStateVisualId(matches[0]) : null;
    }

    private StateMachineStateNode? TryGetState(string visualId)
    {
        try { return _session?.GetState(visualId); }
        catch (KeyNotFoundException) { return null; }
    }

    private StateMachineTransitionEdge? TryGetTransition(string visualId)
    {
        try { return _session?.GetTransition(visualId); }
        catch (KeyNotFoundException) { return null; }
    }

    private int GetConnectedTransitionCount(string visualId)
    {
        var state = TryGetState(visualId);
        return state == null || _session == null ? 0 : _session.Graph.Transitions.Count(x => x.From == state.Name || x.To == state.Name);
    }

    private bool IsSelectedStateInitial() =>
        SelectedState != null && string.Equals(_session?.Graph.InitialState, SelectedState.Name, StringComparison.Ordinal);

    private bool IsSelectedStateCurrent() =>
        SelectedState != null && string.Equals(_session?.Graph.CurrentState, SelectedState.Name, StringComparison.Ordinal);

    private int GetSelectedStateIncomingTransitionCount() => SelectedState == null || _session == null
        ? 0
        : _session.Graph.Transitions.Count(x => string.Equals(x.To, SelectedState.Name, StringComparison.Ordinal));

    private int GetSelectedStateOutgoingTransitionCount() => SelectedState == null || _session == null
        ? 0
        : _session.Graph.Transitions.Count(x => string.Equals(x.From, SelectedState.Name, StringComparison.Ordinal));

    private IReadOnlyCollection<StateMachineValidationIssue> GetSelectedStateIssues() =>
        _session == null || SelectedState == null
            ? []
            : _session.ValidationIssues.Where(x => IsIssueForTarget(x, GetStateIssueTarget(SelectedState))).ToList();

    private IReadOnlyCollection<StateMachineValidationIssue> GetSelectedTransitionIssues() =>
        _session == null || SelectedTransition == null
            ? []
            : _session.ValidationIssues.Where(x => IsIssueForTarget(x, GetTransitionIssueTarget(SelectedTransition))).ToList();

    private static string GetStateIssueTarget(StateMachineStateNode state) =>
        string.IsNullOrWhiteSpace(state.Name) ? "state" : state.Name;

    private static string GetTransitionIssueTarget(StateMachineTransitionEdge transition) =>
        NormalizeOptional(transition.DisplayName) ?? NormalizeOptional(transition.Name) ?? $"{transition.From}->{transition.To}";

    private static bool IsIssueForTarget(StateMachineValidationIssue issue, string target) =>
        string.Equals(issue.Target, target, StringComparison.Ordinal) ||
        issue.Target?.StartsWith($"{target}.", StringComparison.Ordinal) == true;

    private async Task SelectRootActivityForPropertiesAsync()
    {
        var activity = _session?.CanExport == true ? _session.Export() : StateMachine;
        _selectedActivityId = GetActivitySelectionId(activity);
        _selectedSlotName = null;
        if (ActivitySelected.HasDelegate) await ActivitySelected.InvokeAsync(activity);
    }

    private async Task SelectSlotActivityForPropertiesAsync(JsonObject activity)
    {
        _selectedActivityId = GetActivitySelectionId(activity);
        if (ActivitySelected.HasDelegate) await ActivitySelected.InvokeAsync(activity);
    }

    private JsonObject CreateSlotActivity(ActivityDescriptor descriptor)
    {
        var activityId = IdentityGenerator.GenerateId();
        var activity = new JsonObject
        {
            ["id"] = activityId,
            ["nodeId"] = $"{StateMachine.GetNodeId()}:{activityId}",
            ["name"] = ActivityNameGenerator.GenerateNextName(GetIndexedSlotActivities().ToList(), descriptor),
            ["type"] = descriptor.TypeName,
            ["version"] = descriptor.Version
        };
        foreach (var property in descriptor.ConstructionProperties)
            activity.SetProperty(JsonSerializer.SerializeToNode(property.Value), property.Key.Camelize());
        return activity;
    }

    private IEnumerable<JsonObject> GetIndexedSlotActivities()
    {
        if (_session == null) yield break;
        var slots = _session.Graph.States.SelectMany(x => new[] { x.Entry, x.Exit })
            .Concat(_session.Graph.Transitions.SelectMany(x => new[] { x.Trigger, x.Action }));
        foreach (var slot in slots)
        foreach (var activity in EnumerateActivities(slot))
            yield return activity;
    }

    private bool TryFindSlotActivity(string id, out JsonObject activity, out StateMachineStateNode? state, out StateMachineTransitionEdge? transition, out string? slotName)
    {
        if (_session != null)
        {
            foreach (var candidate in _session.Graph.States)
            {
                if (TryFindActivity(candidate.Entry, id, out activity)) { state = candidate; transition = null; slotName = "entry"; return true; }
                if (TryFindActivity(candidate.Exit, id, out activity)) { state = candidate; transition = null; slotName = "exit"; return true; }
            }
            foreach (var candidate in _session.Graph.Transitions)
            {
                if (TryFindActivity(candidate.Trigger, id, out activity)) { state = null; transition = candidate; slotName = "trigger"; return true; }
                if (TryFindActivity(candidate.Action, id, out activity)) { state = null; transition = candidate; slotName = "action"; return true; }
            }
        }
        activity = []; state = null; transition = null; slotName = null; return false;
    }

    private bool TryUpdateSlotActivity(string id, JsonObject replacement)
    {
        if (_session == null) return false;
        foreach (var state in _session.Graph.States)
        {
            if (TryCreateUpdatedSlotValue(state.Entry, id, replacement, out var updatedEntry))
            {
                _session.SetStateSlot(_session.GetStateVisualId(state), StateMachineStateSlot.Entry, updatedEntry);
                return true;
            }

            if (TryCreateUpdatedSlotValue(state.Exit, id, replacement, out var updatedExit))
            {
                _session.SetStateSlot(_session.GetStateVisualId(state), StateMachineStateSlot.Exit, updatedExit);
                return true;
            }
        }
        foreach (var transition in _session.Graph.Transitions)
        {
            if (TryCreateUpdatedSlotValue(transition.Trigger, id, replacement, out var updatedTrigger))
            {
                _session.SetTransitionSlot(_session.GetTransitionVisualId(transition), StateMachineTransitionSlot.Trigger, updatedTrigger);
                return true;
            }

            if (TryCreateUpdatedSlotValue(transition.Action, id, replacement, out var updatedAction))
            {
                _session.SetTransitionSlot(_session.GetTransitionVisualId(transition), StateMachineTransitionSlot.Action, updatedAction);
                return true;
            }
        }
        return false;
    }

    private void TrackParameterState(JsonObject activity, IDictionary<string, ActivityStats>? activityStats)
    {
        _loadedParameterStateMachine = activity;
        _loadedParameterActivityStats = activityStats;
    }

    private static JsonNode? ParseJsonSlot(string? value, string slotName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return JsonNode.Parse(value); }
        catch
        {
            return new JsonObject
            {
                [InvalidJsonSlotProperty] = InvalidJsonSlotMarkerValue,
                ["slot"] = slotName,
                [InvalidJsonSlotSourceProperty] = value
            };
        }
    }

    private static StateMachineStateSlot ParseStateSlot(string name) => name == "entry" ? StateMachineStateSlot.Entry : StateMachineStateSlot.Exit;
    private static StateMachineTransitionSlot ParseTransitionSlot(string name) => name switch
    {
        "trigger" => StateMachineTransitionSlot.Trigger,
        "condition" => StateMachineTransitionSlot.Condition,
        _ => StateMachineTransitionSlot.Action
    };

    private static JsonNode? GetStateSlot(StateMachineStateNode state, string name) => name == "entry" ? state.Entry : state.Exit;
    private static JsonNode? GetTransitionSlot(StateMachineTransitionEdge transition, string name) => name switch
    {
        "trigger" => transition.Trigger,
        "condition" => transition.Condition,
        "action" => transition.Action,
        _ => null
    };

    private static bool TryFindActivity(JsonNode? node, string id, out JsonObject activity)
    {
        if (node is JsonObject obj)
        {
            if (obj.IsActivity() && (obj.GetId() == id || obj.GetNodeId() == id))
            {
                activity = obj;
                return true;
            }

            foreach (var child in obj.Select(x => x.Value))
            {
                if (TryFindActivity(child, id, out activity))
                    return true;
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
            {
                if (TryFindActivity(child, id, out activity))
                    return true;
            }
        }

        activity = [];
        return false;
    }

    private static bool TryCreateUpdatedSlotValue(JsonNode? slot, string id, JsonObject replacement, out JsonNode? updated)
    {
        updated = slot?.DeepClone();
        if (updated == null || !TryReplaceActivity(updated, id, replacement))
        {
            updated = null;
            return false;
        }

        return true;
    }

    private static bool TryReplaceActivity(JsonNode node, string id, JsonObject replacement)
    {
        if (node is JsonObject obj)
        {
            if (obj.IsActivity() && (ReferenceEquals(obj, replacement) || obj.GetId() == id || obj.GetNodeId() == id))
            {
                ReplaceJsonObjectContents(obj, replacement);
                return true;
            }

            foreach (var child in obj.Select(x => x.Value))
            {
                if (child != null && TryReplaceActivity(child, id, replacement))
                    return true;
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
            {
                if (child != null && TryReplaceActivity(child, id, replacement))
                    return true;
            }
        }

        return false;
    }

    private static IEnumerable<JsonObject> EnumerateActivities(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj.IsActivity())
                yield return obj;

            foreach (var child in obj.Select(x => x.Value))
            foreach (var activity in EnumerateActivities(child))
                yield return activity;
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
            foreach (var activity in EnumerateActivities(child))
                yield return activity;
        }
    }

    private static void ReplaceJsonObjectContents(JsonObject target, JsonObject replacement)
    {
        if (ReferenceEquals(target, replacement))
            return;

        target.Clear();
        foreach (var property in replacement)
            target[property.Key] = property.Value?.DeepClone();
    }

    private static string? GetActivitySelectionId(JsonObject activity) => NormalizeOptional(activity.GetId()) ?? NormalizeOptional(activity.GetNodeId());
    private static string? NormalizeOptional(object? value) => string.IsNullOrWhiteSpace(value?.ToString()) ? null : value.ToString();
    private static string DisplayValue(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
    private string GetViewButtonClass(bool outline) => _showOutline == outline ? "state-machine-designer__view-button state-machine-designer__view-button--active" : "state-machine-designer__view-button";
    private static string ToAriaPressed(bool value) => value ? "true" : "false";
    private static string GetIssueClass(StateMachineValidationIssue issue) => issue.Severity == StateMachineValidationSeverity.Error ? "state-machine-designer__issue state-machine-designer__issue--error" : "state-machine-designer__issue";

    private static string? GetString(JsonElement element, string property) => element.TryGetProperty(property, out var value) ? value.GetString() : null;
    private static double GetDouble(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.TryGetDouble(out var number) ? number : 0;
    private static bool TryGetEndpointCell(JsonElement edge, string endpoint, out string cellId)
    {
        if (edge.TryGetProperty(endpoint, out var value) && value.TryGetProperty("cell", out var cell))
        {
            cellId = cell.GetString() ?? "";
            return cellId.Length > 0;
        }
        cellId = "";
        return false;
    }

    private static IReadOnlyList<StateMachineCanvasPosition> ReadVertices(JsonElement edge)
    {
        if (!edge.TryGetProperty("vertices", out var vertices) || vertices.ValueKind != JsonValueKind.Array) return [];
        return vertices.EnumerateArray().Select(x => new StateMachineCanvasPosition(GetDouble(x, "x"), GetDouble(x, "y"))).ToList();
    }

    private static bool IsSyntheticStateId(string id) => id.StartsWith("missing-", StringComparison.Ordinal);
}
