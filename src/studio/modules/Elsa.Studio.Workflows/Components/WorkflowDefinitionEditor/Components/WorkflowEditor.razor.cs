using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.DiagramDesigners;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using Radzen;
using Radzen.Blazor;
using System.Text.Json.Nodes;
using ThrottleDebounce;
using DialogOptions = MudBlazor.DialogOptions;
using DialogPosition = MudBlazor.DialogPosition;
using Variant = MudBlazor.Variant;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// A component that allows the user to edit a workflow definition.
public partial class WorkflowEditor : WorkflowEditorComponentBase, INotificationHandler<ImportedWorkflowDefinition>, IDisposable
{
    private readonly RateLimitedFunc<(bool ReadDiagram, long WorkflowDefinitionGeneration), Task> _rateLimitedSaveChangesAsync;
    private bool _isDirty;
    private bool _disposed;
    private WorkflowDefinition? _lastImportedWorkflowDefinition;
    private long _workflowDefinitionGeneration;
    private long _progressOperationSequence;
    private long? _progressOperationOwner;
    private RadzenSplitterPane _activityPropertiesPane = null!;
    private int _activityPropertiesPaneHeight = 300;
    private DiagramDesignerWrapper _diagramDesigner = null!;

    /// <summary>
    /// The root activity exactly as the server last delivered it. For a BPMN-imported workflow it is the only graph the
    /// ordinary save may send back; see <see cref="SaveAsync"/>.
    /// </summary>
    private JsonObject? _serverRoot;

    /// <summary>
    /// The BPMN document of a BPMN-imported workflow — the only place its bindings are edited — or <see langword="null"/>
    /// for any other workflow. See <see cref="AdoptServerDefinition"/>.
    /// </summary>
    private BpmnDocumentSession? _bpmnDocumentSession;

    private BpmnElementSelection? _selectedBpmnElement;
    private EventCallback<BpmnElementSelection?> _bpmnElementSelected;

    /// <inheritdoc />
    public WorkflowEditor()
    {
        _rateLimitedSaveChangesAsync = Debouncer.Debounce<(bool ReadDiagram, long WorkflowDefinitionGeneration), Task>(
            request => SaveChangesAsync(request.ReadDiagram, false, false, workflowDefinitionGeneration: request.WorkflowDefinitionGeneration),
            TimeSpan.FromMilliseconds(500));
    }

    /// Gets or sets the drag and drop manager via property injection.
    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = null!;

    /// Gets or sets the workflow definition.
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// Gets or sets a callback invoked when the workflow definition is updated.
    [Parameter] public Func<Task>? WorkflowDefinitionUpdated { get; set; }

    /// Gets or sets a callback invoked when an external workflow definition replaces the current definition.
    [Parameter] public Func<Task>? WorkflowDefinitionReloaded { get; set; }

    /// Gets or sets the event triggered when an activity is selected.
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }

    [Parameter] public bool AutoSave { get; set; }
    [Parameter] public EventCallback<bool> AutoSaveChanged { get; set; }

    /// Gets the selected activity ID.
    public string? SelectedActivityId { get; private set; }

    [Inject] private IWorkflowDefinitionEditorService WorkflowDefinitionEditorService { get; set; } = null!;
    [Inject] private IWorkflowDefinitionImporter WorkflowDefinitionImporter { get; set; } = null!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = null!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] private ILogger<WorkflowDefinitionEditor> Logger { get; set; } = null!;
    [Inject] private IWorkflowJsonDetector WorkflowJsonDetector { get; set; } = null!;
    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = null!;
    [Inject] private IWorkflowCloningDialogService WorkflowCloningService { get; set; } = null!;
    [Inject] private IWorkflowExportDialogService WorkflowExportDialogService { get; set; } = null!;
    [Inject] private IBpmnImportUiService BpmnImportUiService { get; set; } = null!;
    [Inject] private IBpmnInterchangeService BpmnInterchangeService { get; set; } = null!;
    [Inject] private IOptions<WorkflowDefinitionOptions> WorkflowDefinitionOptions { get; set; } = null!;

    /// <summary>
    /// Invoked when the "Ctrl+S" hotkeys are pressed, triggering the save operation.
    /// </summary>
    [JSInvokable] public async Task OnHotKeysCtrlS() => await OnSaveClick();

    /// <summary>
    /// Invoked when the "Ctrl+Shift+S" hotkeys are pressed, triggering the save as operation.
    /// </summary>
    [JSInvokable] public async Task OnHotKeysCtrlShiftS() => await OnSaveAsClick();

    private JsonObject? Activity => _workflowDefinition?.Root;
    private bool IsDirty => _isDirty || _bpmnDocumentSession?.IsDirty == true;
    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private ActivityPropertiesPanel? ActivityPropertiesPanel { get; set; }
    private IDictionary<string, object?> WorkflowDefinitionEditorToolbarActionAttributes => new Dictionary<string, object?>
    {
        ["WorkflowDefinition"] = _workflowDefinition,
        ["GetWorkflowDefinition"] = (Func<Task<WorkflowDefinition?>>)GetWorkflowDefinitionSnapshotAsync
    };

    private DotNetObjectReference<WorkflowEditor>? _dotNetRef;

    private RadzenSplitterPane ActivityPropertiesPane
    {
        get => _activityPropertiesPane;
        set
        {
            _activityPropertiesPane = value;

            // Prefix the ID with a non-numerical value, so it can always be used as a query selector
            // (sometimes, Radzen generates a unique ID starting with a number).
            _activityPropertiesPane.UniqueID = $"pane-{value.UniqueID}";
        }
    }

    /// <summary>
    /// Applies the specified workflow definition asynchronously to the current context.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition to apply. Cannot be null.</param>
    public async Task ApplyWorkflowDefinitionAsync(WorkflowDefinition workflowDefinition)
    {
        await SetWorkflowDefinitionAsync(workflowDefinition, true, isServerDefinition: false);
        await HandleChangesAsync(false, true);
    }

    /// Gets or sets a flag indicating whether the workflow definition is dirty.
    public async Task NotifyWorkflowChangedAsync(bool force = false)
    {
        await HandleChangesAsync(false, force);
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Mediator.Subscribe<ImportedWorkflowDefinition>(this);

        _bpmnElementSelected = EventCallback.Factory.Create<BpmnElementSelection?>(this, OnBpmnElementSelectedAsync);
        _workflowDefinition = WorkflowDefinition;
        AdoptServerDefinition(_workflowDefinition);

        await ActivityRegistry.EnsureLoadedAsync();

        if (_workflowDefinition?.Root == null)
            return;

        await SelectActivityAsync(_workflowDefinition.Root);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (_disposed)
            return;

        if (ReferenceEquals(WorkflowDefinition, _workflowDefinition))
            return;

        InvalidateWorkflowDefinitionOperations();

        var workflowDefinition = _workflowDefinition = WorkflowDefinition;
        var expectedWorkflowDefinitionGeneration = _workflowDefinitionGeneration;
        AdoptServerDefinition(workflowDefinition);

        if (workflowDefinition?.Root == null)
            return;

        try
        {
            await _diagramDesigner.LoadActivityAsync(workflowDefinition.Root);
        }
        catch (Exception) when (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration) || !ReferenceEquals(_workflowDefinition, workflowDefinition))
        {
            return;
        }

        if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration) || !ReferenceEquals(_workflowDefinition, workflowDefinition))
            return;

        await SelectActivityAsync(workflowDefinition.Root);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await UpdateActivityPropertiesVisibleHeightAsync();
            _dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("editorHotkeys.register", _dotNetRef);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeCore();
    }

    private void DisposeCore()
    {
        if (_disposed)
            return;

        _disposed = true;
        _workflowDefinitionGeneration++;
        _progressOperationOwner = null;
        IsProgressing = false;
        Mediator.Unsubscribe(this);
        _rateLimitedSaveChangesAsync.Dispose();
    }

    /// Invalidates asynchronous operations that belong to the current workflow definition.
    public void InvalidateWorkflowDefinitionOperations()
    {
        _workflowDefinitionGeneration++;
        _progressOperationOwner = null;
        IsProgressing = false;
    }

    private async Task HandleChangesAsync(bool readDiagram, bool force = false)
    {
        var workflowDefinitionGeneration = _workflowDefinitionGeneration;
        _isDirty = true;
        await InvokeAsync(StateHasChanged);

        if (AutoSave)
            if (force)
                await SaveChangesAsync(readDiagram, showLoader: false, publish: false, workflowDefinitionGeneration: workflowDefinitionGeneration);
            else
                await SaveChangesRateLimitedAsync(readDiagram, workflowDefinitionGeneration);
    }

    private async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(bool readDiagram, bool publish, long workflowDefinitionGeneration)
    {
        var workflowDefinition = _workflowDefinition ?? new WorkflowDefinition();

        if (_bpmnDocumentSession != null)
        {
            // A BPMN-imported workflow's graph is derived from its BPMN document, and elsa-core refuses to read or export
            // that document once an ordinary save has rewritten the graph behind it. Bindings are edited in the document
            // and saved through the document PUT (SaveBpmnDocumentBackedDefinitionAsync); this save may carry the other
            // properties only, sending the graph back exactly as the server delivered it. The designer is never read:
            // the BPMN canvas cannot edit the graph, so it could only ever hand back this same root.
            if (!JsonNode.DeepEquals(workflowDefinition.Root, _serverRoot))
                return new(new ValidationErrors([new(Localizer["This workflow's activities come from its BPMN document, and this save would change them outside it, so nothing was saved. Bind tasks in the Performed by panel instead, and reload the workflow to discard the other activity changes."])]));
        }
        else if (readDiagram)
        {
            var root = await _diagramDesigner.GetActivityAsync();

            if (!IsCurrentWorkflowOperation(workflowDefinitionGeneration))
                throw new OperationCanceledException();

            workflowDefinition.Root = root;
        }

        var result = await WorkflowDefinitionEditorService.SaveAsync(workflowDefinition, publish, async definition =>
        {
            if (IsCurrentWorkflowOperation(workflowDefinitionGeneration))
                await SetWorkflowDefinitionAsync(definition);
        });

        if (IsCurrentWorkflowOperation(workflowDefinitionGeneration))
        {
            _isDirty = false;
            await InvokeAsync(StateHasChanged);
        }

        return result;
    }

    private async Task PublishAsync(Func<SaveWorkflowDefinitionResponse, Task>? onSuccess = null, Func<ValidationErrors, Task>? onFailure = null)
    {
        await SaveChangesAsync(true, true, true, onSuccess, onFailure);
    }

    private async Task RetractAsync(Func<Task>? onSuccess = null, Func<ValidationErrors, Task>? onFailure = null)
    {
        var workflowDefinitionGeneration = _workflowDefinitionGeneration;
        var result = await WorkflowDefinitionEditorService.RetractAsync(_workflowDefinition!, async definition =>
        {
            if (workflowDefinitionGeneration == _workflowDefinitionGeneration)
                await SetWorkflowDefinitionAsync(definition);
        });

        if (workflowDefinitionGeneration != _workflowDefinitionGeneration)
            return;

        await result.OnSuccessAsync(async _ =>
        {
            if (onSuccess != null) await onSuccess();
        });

        await result.OnFailedAsync(async errors =>
        {
            if (onFailure != null) await onFailure(errors);
        });
    }

    private async Task SaveChangesRateLimitedAsync(bool readDiagram, long workflowDefinitionGeneration)
    {
        await _rateLimitedSaveChangesAsync!.InvokeAsync((readDiagram, workflowDefinitionGeneration));
    }

    private async Task SaveChangesAsync(bool readDiagram, bool showLoader, bool publish, Func<SaveWorkflowDefinitionResponse, Task>? onSuccess = null, Func<ValidationErrors, Task>? onFailure = null, long? workflowDefinitionGeneration = null)
    {
        var expectedWorkflowDefinitionGeneration = workflowDefinitionGeneration ?? _workflowDefinitionGeneration;

        if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
            return;

        var progressOperationOwner = showLoader ? BeginProgressOperation() : null;

        // Because this method is rate-limited, it's possible that the designer has been disposed of since the last invocation.
        // Therefore, we need to wrap this in a try/catch block.
        try
        {
            if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                return;

            if (progressOperationOwner is { } progressOwner)
            {
                try
                {
                    await InvokeAsync(() =>
                    {
                        if (!IsCurrentProgressOperation(expectedWorkflowDefinitionGeneration, progressOwner))
                            return;

                        IsProgressing = true;
                        StateHasChanged();
                    });
                }
                catch (ObjectDisposedException) when (_disposed)
                {
                    return;
                }
                catch (InvalidOperationException) when (_disposed)
                {
                    return;
                }
            }

            if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                return;

            var result = await SaveAsync(readDiagram, publish, expectedWorkflowDefinitionGeneration);

            if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                return;

            await result.OnSuccessAsync(async response =>
            {
                if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                    return;

                var currentSelectedActivityId = SelectedActivityId;

                await SetWorkflowDefinitionAsync(response.WorkflowDefinition);

                if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                    return;

                // The save may have moved what the document's ETag covers (a new draft version, say), or left the
                // document stale; reading it again says which, rather than letting the next binding save find out. A
                // working copy with unsaved edits is left alone: its save reports a moved ETag as a conflict itself.
                if (_bpmnDocumentSession is { IsDirty: false } bpmnDocumentSession)
                    await bpmnDocumentSession.ReloadAsync();

                if (!string.IsNullOrEmpty(currentSelectedActivityId))
                {
                    await RefreshSelectedActivityAsync(currentSelectedActivityId);

                    if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                        return;
                }

                await InvokeAsync(() =>
                {
                    if (IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                        StateHasChanged();
                });

                if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                    return;

                if (onSuccess != null)
                    await onSuccess(response);

            }).ConfigureAwait(false);

            await result.OnFailedAsync(errors =>
            {
                if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                    return Task.CompletedTask;

                onFailure?.Invoke(errors);
                UserMessageService.ShowSnackbarTextMessage(
                    errors.Errors.Select(x => x.ErrorMessage),
                    Severity.Error,
                    options => options.VisibleStateDuration = 5000
                );
                return Task.CompletedTask;
            });
        }
        catch (DiagramDesignerValidationException e)
        {
            if (!IsCurrentWorkflowOperation(expectedWorkflowDefinitionGeneration))
                return;

            UserMessageService.ShowSnackbarTextMessage(
                e.Message,
                Severity.Error,
                options => options.VisibleStateDuration = 5000
            );
        }
        catch (OperationCanceledException) when (expectedWorkflowDefinitionGeneration != _workflowDefinitionGeneration)
        {
            Logger.LogDebug("Skipped obsolete workflow save after the workflow definition changed.");
        }
        finally
        {
            if (progressOperationOwner is { } progressOwner)
                await CompleteProgressOperationAsync(expectedWorkflowDefinitionGeneration, progressOwner);
        }
    }

    private long? BeginProgressOperation() => _progressOperationOwner = ++_progressOperationSequence;

    private bool IsCurrentWorkflowOperation(long workflowDefinitionGeneration) => !_disposed && workflowDefinitionGeneration == _workflowDefinitionGeneration;

    private bool IsCurrentProgressOperation(long workflowDefinitionGeneration, long progressOperationOwner) =>
        IsCurrentWorkflowOperation(workflowDefinitionGeneration) && _progressOperationOwner == progressOperationOwner;

    private async Task CompleteProgressOperationAsync(long workflowDefinitionGeneration, long progressOperationOwner)
    {
        if (_disposed)
        {
            if (_progressOperationOwner == progressOperationOwner)
                _progressOperationOwner = null;

            return;
        }

        try
        {
            await InvokeAsync(() =>
            {
                if (_progressOperationOwner != progressOperationOwner)
                    return;

                _progressOperationOwner = null;
                IsProgressing = false;

                if (IsCurrentWorkflowOperation(workflowDefinitionGeneration))
                    StateHasChanged();
            });
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            if (_progressOperationOwner == progressOperationOwner)
                _progressOperationOwner = null;

            Logger.LogDebug("Skipped obsolete workflow progress cleanup after editor disposal.");
        }
        catch (InvalidOperationException) when (_disposed)
        {
            if (_progressOperationOwner == progressOperationOwner)
                _progressOperationOwner = null;

            Logger.LogDebug("Skipped obsolete workflow progress cleanup after editor disposal.");
        }
    }

    private async Task SelectActivityAsync(JsonObject activity)
    {
        SelectedActivity = activity;
        SelectedActivityId = activity.GetId();
        ActivityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion());
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetWorkflowDefinitionAsync(WorkflowDefinition workflowDefinition, bool isExternalReplacement = false, bool isServerDefinition = true)
    {
        if (isExternalReplacement)
            InvalidateWorkflowDefinitionOperations();

        _workflowDefinition = WorkflowDefinition = workflowDefinition;

        if (isServerDefinition)
            AdoptServerDefinition(workflowDefinition);

        if (isExternalReplacement && WorkflowDefinitionReloaded != null)
            await WorkflowDefinitionReloaded();
        else if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();
    }

    internal async Task SetImportedWorkflowDefinitionAsync(WorkflowDefinition workflowDefinition)
    {
        var isNewImport = !ReferenceEquals(_lastImportedWorkflowDefinition, workflowDefinition);
        _lastImportedWorkflowDefinition = workflowDefinition;
        await SetWorkflowDefinitionAsync(workflowDefinition, isNewImport);

        if (isNewImport && workflowDefinition.Root != null)
            await _diagramDesigner.LoadActivityAsync(workflowDefinition.Root);
    }

    private async Task<WorkflowDefinition?> GetWorkflowDefinitionSnapshotAsync()
    {
        if (_workflowDefinition is null)
            return null;

        _workflowDefinition.Root = await _diagramDesigner.GetActivityAsync();
        return _workflowDefinition;
    }

    /// <summary>
    /// Refreshes the selected activity reference to point to the updated activity in the current workflow definition.
    /// This is needed after saving changes to ensure the UI displays the latest activity state.
    /// </summary>
    private async Task RefreshSelectedActivityAsync(string activityId)
    {
        if (_workflowDefinition?.Root == null || string.IsNullOrEmpty(activityId))
            return;

        // Find the updated activity in the current workflow definition
        var updatedActivity = await FindActivityByIdAsync(_workflowDefinition.Root, activityId);

        if (updatedActivity != null)
        {
            // Update the selected activity reference without triggering a disruptive UI refresh.
            // We avoid calling SelectActivity() which sets the activity to null and calls StateHasChanged() multiple times.
            SelectedActivity = updatedActivity;
            SelectedActivityId = updatedActivity.GetId();
            ActivityDescriptor = ActivityRegistry.Find(updatedActivity.GetTypeName(), updatedActivity.GetVersion());

            // Only call StateHasChanged once to update the UI without disrupting focus
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Recursively searches for an activity with the specified ID in the activity tree.
    /// </summary>
    private async Task<JsonObject?> FindActivityByIdAsync(JsonObject rootActivity, string activityId)
    {
        // Use the activity visitor to traverse the entire activity graph
        var activityGraph = await ActivityVisitor.VisitAndCreateGraphAsync(rootActivity);
        
        // Look for the activity in the activity node lookup
        foreach (var node in activityGraph.ActivityNodeLookup.Values)
        {
            if (node.Activity.GetId() == activityId)
            {
                return node.Activity;
            }
        }
        
        return null;
    }

    private async Task UpdateActivityPropertiesVisibleHeightAsync()
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _activityPropertiesPaneHeight = (int)visibleHeight - 50;
    }

    private async Task OnActivitySelected(JsonObject activity)
    {
        await SelectActivityAsync(activity);
        if (ActivitySelected != null) await ActivitySelected(activity);
    }

    private async Task OnSelectedActivityUpdated(JsonObject activity)
    {
        _isDirty = true;
        StateHasChanged();
        await _diagramDesigner.UpdateActivityAsync(SelectedActivityId!, activity);
    }

    private async Task OnSaveClick()
    {
        if (_bpmnDocumentSession != null)
        {
            await SaveBpmnDocumentBackedDefinitionAsync();
            return;
        }

        await SaveChangesAsync(true, true, false, _ =>
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow saved"], Severity.Success);
            return Task.CompletedTask;
        });
    }

    private async Task OnSaveAsClick()
    {
        if (WorkflowDefinition is null)
            return;

        var result = await WorkflowCloningService.SaveAs(WorkflowDefinition);
        if (result is null) return;
        var definitionId = result.Success?.WorkflowDefinition.DefinitionId;
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(definitionId))
            NavigationManager.NavigateTo($"workflows/definitions/{definitionId}/edit");
    }

    private async Task OnPublishClicked()
    {
        // Publishing would publish the graph the server has now, without the unsaved binding changes.
        if (_bpmnDocumentSession?.IsDirty == true)
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["Save or discard the binding changes before publishing."], Severity.Warning);
            return;
        }

        await ProgressAsync(async () => await PublishAsync(async response =>
        {
            // Depending on whether the workflow contains Not Found activities, display a different message.
            var graph = await _diagramDesigner.GetActivityGraphAsync();
            var nodes = graph.ActivityNodeLookup.Values;
            var hasNotFoundActivities = nodes.Any(x => x.Activity.GetTypeName() == "Elsa.NotFoundActivity");

            if (hasNotFoundActivities)
                UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow published with Not Found activities"], Severity.Warning, options => options.VisibleStateDuration = 5000);
            else
                UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow published"], Severity.Success);

            if (response.ConsumingWorkflowCount > 0)
            {
                UserMessageService.ShowSnackbarTextMessage(Localizer["{0} consuming workflow(s) updated", response.ConsumingWorkflowCount], Severity.Success);
            }
        }));
    }

    private async Task OnRetractClicked()
    {
        await ProgressAsync(async () => await RetractAsync(() =>
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow unpublished"], Severity.Success);
            return Task.CompletedTask;
        }, errors =>
        {
            UserMessageService.ShowSnackbarTextMessage(string.Join(Environment.NewLine, errors), Severity.Error);
            return Task.CompletedTask;
        }));
    }
    
    /// <summary>
    /// Records <paramref name="workflowDefinition"/> as the server delivered it. For a BPMN-imported workflow — one
    /// whose root is an <c>Elsa.BpmnProcess</c> and that carries its BPMN source — that is two things: the root exactly
    /// as sent, the only graph an ordinary save of it may send back, and the session holding its BPMN document, the
    /// only place its bindings are edited. Never called for a definition edited locally, such as one applied from the
    /// code view.
    /// </summary>
    private void AdoptServerDefinition(WorkflowDefinition? workflowDefinition)
    {
        _serverRoot = (JsonObject?)workflowDefinition?.Root?.DeepClone();

        // workflowDefinition is never null once it is BPMN-document-backed: IsBpmnDocumentBacked is null-safe and
        // returns false for a null definition, so the flow analysis below can treat it as non-null from here on.
        if (workflowDefinition is null || !IsBpmnDocumentBacked(workflowDefinition))
        {
            _bpmnDocumentSession = null;
            _selectedBpmnElement = null;
            return;
        }

        if (_bpmnDocumentSession?.DefinitionId == workflowDefinition.DefinitionId)
            return;

        _bpmnDocumentSession = new(BpmnInterchangeService, workflowDefinition.DefinitionId);
        _selectedBpmnElement = null;
    }

    private static bool IsBpmnDocumentBacked(WorkflowDefinition? workflowDefinition) =>
        workflowDefinition?.Root?.GetTypeName() == BpmnProcessConstants.ActivityTypeName
        && workflowDefinition.CustomProperties.ContainsKey(BpmnProcessConstants.SourceXmlCustomPropertyKey);

    /// <summary>
    /// Saves a BPMN-imported workflow: unsaved changes to its other properties first, through the ordinary save (which
    /// sends the graph back unchanged), then unsaved binding changes through the document PUT, against the ETag of the
    /// document they were made in. In that order because a successful PUT reloads the definition, which would otherwise
    /// replace properties nobody had saved yet.
    /// </summary>
    private async Task SaveBpmnDocumentBackedDefinitionAsync()
    {
        var session = _bpmnDocumentSession!;
        var ordinarySavePerformed = false;

        if (_isDirty || !session.IsDirty)
        {
            var saved = false;

            await SaveChangesAsync(false, true, false, _ =>
            {
                saved = true;
                return Task.CompletedTask;
            });

            // A refused save has already said why; saving the bindings after it would reload the definition over the
            // properties it did not save.
            if (!saved || !session.IsDirty)
            {
                if (saved)
                    UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow saved"], Severity.Success);

                return;
            }

            ordinarySavePerformed = true;
        }

        await ProgressAsync(async () =>
        {
            // The ordinary save above may have advanced the document's ETag without changing what it describes — it
            // re-serializes the root, and a publish opens a new draft version — so the PUT below would otherwise carry
            // a stale If-Match and be refused for a change nobody made. Adopting the new ETag first, when the content
            // is unchanged, avoids that; a genuine conflict is still reported as one.
            if (ordinarySavePerformed && !await session.RefreshRevisionAfterOrdinarySaveAsync())
            {
                UserMessageService.ShowSnackbarTextMessage(Localizer["The binding changes were not saved."], Severity.Error);
                return;
            }

            var result = await session.SaveAsync();

            if (_disposed)
                return;

            if (!result.IsSuccess)
            {
                UserMessageService.ShowSnackbarTextMessage(Localizer["The binding changes were not saved."], Severity.Error);
                return;
            }

            if (!await ReloadDefinitionFromServerAsync())
                return;

            if (session.SavedButReloadFailed)
                UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow saved, but its BPMN document could not be reloaded. Use Reload in the Performed by panel to see its current bindings."], Severity.Warning);
            else
                UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow saved"], Severity.Success);
        });
    }

    /// <summary>
    /// Reloads a BPMN-imported workflow and its document from the server, discarding every unsaved change: what a user
    /// does after the document was changed by someone else, or went stale.
    /// </summary>
    private async Task ReloadBpmnDocumentBackedDefinitionAsync()
    {
        await ProgressAsync(async () =>
        {
            if (await ReloadDefinitionFromServerAsync() && _bpmnDocumentSession != null)
                await _bpmnDocumentSession.ReloadAsync();
        });
    }

    /// <summary>
    /// Replaces the edited definition with the server's latest version — after a document PUT, the graph it re-imported
    /// — and redraws the designer from it, exactly as an import into this definition does.
    /// </summary>
    private async Task<bool> ReloadDefinitionFromServerAsync()
    {
        var definition = await WorkflowDefinitionService.FindByDefinitionIdAsync(_workflowDefinition!.DefinitionId, VersionOptions.Latest);

        if (definition == null)
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["The workflow definition could not be reloaded."], Severity.Error);
            return false;
        }

        _isDirty = false;
        await SetImportedWorkflowDefinitionAsync(definition);
        return true;
    }

    private async Task OnBpmnElementSelectedAsync(BpmnElementSelection? selection)
    {
        _selectedBpmnElement = selection;
        await InvokeAsync(StateHasChanged);
    }

    private Task OnBpmnDocumentChangedAsync() => InvokeAsync(StateHasChanged);

    private async Task OnGraphUpdated() => await HandleChangesAsync(true);
    
    private async Task OnActivityUpdated(JsonObject activity)
    {
        await HandleChangesAsync(true);
    }

    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        await UpdateActivityPropertiesVisibleHeightAsync();
    }

    private async Task OnAutoSaveChanged(bool? value)
    {
        AutoSave = value ?? false;
        if (AutoSaveChanged.HasDelegate)
            await AutoSaveChanged.InvokeAsync(AutoSave);

        if (AutoSave)
            await SaveChangesAsync(true, false, false);
    }

    private async Task OnExportClicked()
    {
        await WorkflowExportDialogService.ExportAndDownloadAsync(include =>
            WorkflowDefinitionEditorService.ExportAsync(_workflowDefinition!, include));
    }

    private async Task OnImportClicked()
    {
        await DomAccessor.ClickElementAsync("#workflow-file-upload-button-wrapper input[type=file]");
    }

    private async Task OnFilesSelected(IReadOnlyList<IBrowserFile>? files)
    {
        if (files == null || files.Count == 0)
            return;

        await ImportFilesAsync(files);
        _isDirty = false;

        StateHasChanged();
    }
    
    async Task INotificationHandler<ImportedWorkflowDefinition>.HandleAsync(ImportedWorkflowDefinition notification, CancellationToken cancellationToken)
    {
        await SetImportedWorkflowDefinitionAsync(notification.WorkflowDefinition);
    }

    private async Task ImportFilesAsync(IReadOnlyList<IBrowserFile> files)
    {
        _isDirty = true;
        IsProgressing = true;
        StateHasChanged();

        var options = new ImportOptions
        {
            DefinitionId = WorkflowDefinition?.DefinitionId,
            ImportedCallback = async definition =>
            {
                await SetImportedWorkflowDefinitionAsync(definition);
            },
            ErrorCallback = ex =>
            {
                UserMessageService.ShowSnackbarTextMessage($"Failed to import workflow definition: {ex.Message}", Severity.Error);
                return Task.CompletedTask;
            }
        };

        // A .bpmn file cannot be parsed as JSON, so it is routed through the same interactive BPMN import flow the
        // definitions list uses, updating the definition currently open here rather than creating a new one.
        var bpmnBatch = await BpmnImportUiService.ImportBpmnFilesAsync(files, options.DefinitionId);

        var importResults = new List<WorkflowImportResult>(bpmnBatch.Results);

        if (bpmnBatch.OtherFiles.Count > 0)
            importResults.AddRange(await WorkflowDefinitionImporter.ImportFilesAsync(bpmnBatch.OtherFiles, options));

        foreach (var bpmnResult in bpmnBatch.Results.Where(x => x.IsSuccess && x.WorkflowDefinition != null))
            await SetImportedWorkflowDefinitionAsync(bpmnResult.WorkflowDefinition!);

        var reportableResults = BpmnImportBatch.ReportableResults(importResults);
        var failedImports = reportableResults.Where(x => !x.IsSuccess).ToList();
        var successfulImports = reportableResults.Where(x => x.IsSuccess).ToList();

        IsProgressing = false;
        _isDirty = false;
        StateHasChanged();

        if (BpmnImportBatch.AllReported(importResults))
        {
            // Every result was a capability refusal, each already explained in its own dialog; nothing left to summarize.
            return;
        }

        if (reportableResults.Count == 0)
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["No workflows were imported."], Severity.Info);
            return;
        }

        if (successfulImports.Count == 1)
            UserMessageService.ShowSnackbarTextMessage(Localizer["Successfully imported 1 workflow definition."], Severity.Success, ConfigureSnackbar);
        else if (reportableResults.Count > 1)
            UserMessageService.ShowSnackbarTextMessage(Localizer["Successfully imported {0} workflow definitions.", reportableResults.Count], Severity.Success, ConfigureSnackbar);

        if (failedImports.Count == 1)
            UserMessageService.ShowSnackbarTextMessage(Localizer["Failed to import 1 workflow definition: {0}", failedImports[0].Failure!.ErrorMessage], Severity.Error, ConfigureSnackbar);
        else if (failedImports.Count > 1)
            UserMessageService.ShowSnackbarTextMessage(Localizer["Failed to import {0} workflow definitions. Errors: {1}", failedImports.Count, string.Join(", ", failedImports.Select(x => x.Failure!.ErrorMessage))], Severity.Error, ConfigureSnackbar);

        return;
        void ConfigureSnackbar(SnackbarOptions snackbarOptions)
        {
            snackbarOptions.SnackbarVariant = Variant.Filled;
            snackbarOptions.CloseAfterNavigation = failedImports.Count > 0;
            snackbarOptions.VisibleStateDuration = failedImports.Count > 0 ? 10000 : 5000;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        DisposeCore();

        if (_dotNetRef != null)
        {
            await JSRuntime.InvokeVoidAsync("editorHotkeys.dispose", _dotNetRef);
            _dotNetRef.Dispose();
            _dotNetRef = null;
        }
    }
}
