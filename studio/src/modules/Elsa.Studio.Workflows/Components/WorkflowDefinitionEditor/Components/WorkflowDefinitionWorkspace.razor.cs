using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// A workspace for editing a workflow definition.
public partial class WorkflowDefinitionWorkspace : IWorkspace, IDisposable
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
    private long _compensationCapabilityGeneration;
    private long _nativeGraphAnalysisGeneration;
    private bool _nativeCompensationDraftCapabilityEnabled;
    private bool _nativeCompensationDraftContextInvalidated;
    private bool _isReloadingNativeCompensationDraft;
    private bool _nativeGraphAnalysisPending;
    private string? _nativeCompensationDraftReloadError;
    private string? _compensationCapabilityContextKey;
    private string? _compensationDocumentContextKey;
    private string? _nativeGraphAnalysisBackendContextKey;
    private string? _nativeGraphAnalysisDocumentContextKey;
    private string? _nativeGraphAnalysisDefinitionId;
    private JsonObject? _nativeGraphAnalysisRootSnapshot;
    private NativeCompensationDraftGraphAnalysis _nativeGraphAnalysis = NativeCompensationDraftGraphAnalysis.Ordinary;
    private CancellationTokenSource? _compensationCapabilityCheck;
    private CancellationTokenSource? _nativeGraphAnalysisCheck;
    private WorkflowEditor WorkflowEditor { get; set; } = null!;
    private CodeView CodeView { get; set; } = null!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private INativeCompensationDraftGraphClassifier NativeDraftGraphClassifier { get; set; } = null!;
    [Inject] private IRemoteFeatureProvider RemoteFeatureProvider { get; set; } = null!;
    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = null!;
    [Inject] private IPermissionRefreshSignal PermissionRefreshSignal { get; set; } = null!;
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

    [CascadingParameter] private UserPermissions Permissions { get; set; } = UserPermissions.Unknown;

    /// <inheritdoc />
    /// <remarks>A user who cannot save workflow definitions gets the read-only viewer and read-only properties.</remarks>
    public bool IsReadOnly => _selectedWorkflowDefinition.GetIsReadOnly() || !Permissions.CanWriteDefinitions() || IsNativeCompensationDraftReadOnly;

    private bool IsNativeCompensationDraftReadOnly
    {
        get
        {
            var root = _selectedWorkflowDefinition?.Root;
            if (!NativeCompensationDraftCapability.ContainsPotentialScopeMarker(root))
                return false;

            if (!IsNativeGraphAnalysisCurrent && !CanRetainVerifiedNativeAnalysisDuringRecheck)
                return true;

            if (!_nativeGraphAnalysis.RequiresNativeProtection)
                return false;

            return _nativeGraphAnalysis.MustRejectProtectedWrite || !HasNativeCompensationDraftCapability ||
                   _compensationDocumentContextKey == null || _compensationDocumentContextKey != BackendApiClientProvider.ContextKey;
        }
    }

    private bool IsNativeCompensationDraftOpaqueOrUnsupported =>
        !IsNativeGraphAnalysisCurrent && !CanRetainVerifiedNativeAnalysisDuringRecheck ||
        _nativeGraphAnalysis.MustRejectProtectedWrite;

    private bool HasAnalyzedNativeCompensationScope => _nativeGraphAnalysis.ContainsCompensationScope;

    private bool HasNativeCompensationDraftCapability =>
        _nativeCompensationDraftCapabilityEnabled && _compensationCapabilityContextKey == BackendApiClientProvider.ContextKey &&
        BackendApiClientProvider is IBackendRequestContextBinding { SupportsAuthenticatedRequestContextBinding: true };

    private bool IsNativeCompensationDraftContextStale =>
        (_nativeGraphAnalysis.RequiresNativeProtection || _nativeGraphAnalysisPending &&
            NativeCompensationDraftCapability.ContainsPotentialScopeMarker(_selectedWorkflowDefinition?.Root)) &&
        (_nativeCompensationDraftContextInvalidated || _compensationDocumentContextKey == null ||
         _compensationDocumentContextKey != BackendApiClientProvider.ContextKey);

    private bool IsNativeGraphAnalysisCurrent =>
        string.Equals(_nativeGraphAnalysisBackendContextKey, BackendApiClientProvider.ContextKey, StringComparison.Ordinal) &&
        string.Equals(_nativeGraphAnalysisDocumentContextKey, _compensationDocumentContextKey, StringComparison.Ordinal) &&
        string.Equals(_nativeGraphAnalysisDefinitionId, _selectedWorkflowDefinition?.DefinitionId, StringComparison.Ordinal) &&
        JsonNode.DeepEquals(_nativeGraphAnalysisRootSnapshot, _selectedWorkflowDefinition?.Root);

    private bool CanRetainVerifiedNativeAnalysisDuringRecheck =>
        _nativeGraphAnalysisPending && _nativeGraphAnalysis.IsComplete && _nativeGraphAnalysis.ContainsCompensationScope &&
        HasVerifiedNativeAnalysisForCurrentOrigin;

    private bool HasVerifiedNativeAnalysisForCurrentOrigin =>
        _nativeGraphAnalysis.IsComplete && _nativeGraphAnalysis.ContainsCompensationScope &&
        !_nativeCompensationDraftContextInvalidated && _compensationDocumentContextKey != null &&
        _compensationDocumentContextKey == BackendApiClientProvider.ContextKey &&
        _nativeGraphAnalysisBackendContextKey == BackendApiClientProvider.ContextKey &&
        _nativeGraphAnalysisDocumentContextKey == _compensationDocumentContextKey &&
        _nativeGraphAnalysisDefinitionId == _selectedWorkflowDefinition?.DefinitionId;

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
        _compensationDocumentContextKey = GetCompensationDocumentContextKey(_selectedWorkflowDefinition);
        PermissionRefreshSignal.Raised += OnBackendContextChanged;
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
        _compensationDocumentContextKey = GetCompensationDocumentContextKey(_selectedWorkflowDefinition);
        _nativeCompensationDraftContextInvalidated = _nativeGraphAnalysis.RequiresNativeProtection &&
                                                     _nativeCompensationDraftContextInvalidated &&
                                                     _compensationDocumentContextKey != BackendApiClientProvider.ContextKey;
        _workflowDefinitionReloadVersion++;
        _nativeCompensationDraftCapabilityEnabled = false;
        _compensationCapabilityContextKey = null;
        _compensationCapabilityGeneration++;
        _compensationCapabilityCheck?.Cancel();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await RefreshNativeCompensationDraftCapabilityAsync();
        await RefreshNativeCompensationDraftGraphAnalysisAsync();
    }

    private async Task RefreshNativeCompensationDraftGraphAnalysisAsync(bool force = false)
    {
        var root = _selectedWorkflowDefinition?.Root;
        if (!NativeCompensationDraftCapability.ContainsPotentialScopeMarker(root))
        {
            CancelNativeGraphAnalysisCheck();
            _nativeGraphAnalysis = NativeCompensationDraftGraphAnalysis.Ordinary;
            _nativeGraphAnalysisRootSnapshot = root?.DeepClone().AsObject();
            _nativeGraphAnalysisBackendContextKey = null;
            _nativeGraphAnalysisDocumentContextKey = _compensationDocumentContextKey;
            _nativeGraphAnalysisDefinitionId = _selectedWorkflowDefinition?.DefinitionId;
            _nativeGraphAnalysisPending = false;
            return;
        }

        if (!force && IsNativeGraphAnalysisCurrent && !_nativeGraphAnalysisPending)
            return;

        var backendContextKey = BackendApiClientProvider.ContextKey;
        var documentContextKey = _compensationDocumentContextKey;
        var definitionId = _selectedWorkflowDefinition?.DefinitionId;
        var rootSnapshot = root?.DeepClone().AsObject();
        var canRetain = HasVerifiedNativeAnalysisForCurrentOrigin;

        CancelNativeGraphAnalysisCheck();
        var cancellationSource = new CancellationTokenSource();
        _nativeGraphAnalysisCheck = cancellationSource;
        var generation = ++_nativeGraphAnalysisGeneration;
        _nativeGraphAnalysisPending = true;
        if (!canRetain)
            _nativeGraphAnalysis = NativeCompensationDraftGraphAnalysis.Indeterminate;

        NativeCompensationDraftGraphAnalysis analysis;
        try
        {
            analysis = await NativeDraftGraphClassifier.AnalyzeAsync(rootSnapshot, cancellationSource.Token);
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
            ReleaseNativeGraphAnalysisCheck(cancellationSource);
            return;
        }
        catch
        {
            analysis = NativeCompensationDraftGraphAnalysis.Indeterminate;
        }

        var isCurrentCheck = generation == _nativeGraphAnalysisGeneration &&
                             ReferenceEquals(_nativeGraphAnalysisCheck, cancellationSource);
        ReleaseNativeGraphAnalysisCheck(cancellationSource);

        if (!isCurrentCheck || BackendApiClientProvider.ContextKey != backendContextKey ||
            _compensationDocumentContextKey != documentContextKey ||
            _selectedWorkflowDefinition?.DefinitionId != definitionId ||
            !JsonNode.DeepEquals(rootSnapshot, _selectedWorkflowDefinition?.Root))
        {
            if (generation == _nativeGraphAnalysisGeneration)
            {
                _nativeGraphAnalysisPending = false;
                _nativeGraphAnalysis = NativeCompensationDraftGraphAnalysis.Indeterminate;
            }

            return;
        }

        _nativeGraphAnalysis = analysis;
        _nativeGraphAnalysisRootSnapshot = rootSnapshot;
        _nativeGraphAnalysisBackendContextKey = backendContextKey;
        _nativeGraphAnalysisDocumentContextKey = documentContextKey;
        _nativeGraphAnalysisDefinitionId = definitionId;
        _nativeGraphAnalysisPending = false;
    }

    private async Task RefreshNativeCompensationDraftCapabilityAsync(bool force = false)
    {
        var documentContextKey = _compensationDocumentContextKey;
        if (documentContextKey == null)
        {
            CancelNativeCompensationDraftCapabilityCheck();
            _nativeCompensationDraftCapabilityEnabled = false;
            _compensationCapabilityContextKey = null;
            return;
        }

        var backendContextKey = BackendApiClientProvider.ContextKey;
        var canRetainCapability = _nativeCompensationDraftCapabilityEnabled &&
                                  _compensationCapabilityContextKey == backendContextKey &&
                                  !_nativeCompensationDraftContextInvalidated;
        if (documentContextKey != backendContextKey)
        {
            CancelNativeCompensationDraftCapabilityCheck();
            _nativeCompensationDraftCapabilityEnabled = false;
            _compensationCapabilityContextKey = null;
            return;
        }

        if (!force && canRetainCapability)
            return;

        if (!force && _compensationCapabilityCheck is { IsCancellationRequested: false } &&
            _compensationCapabilityContextKey == backendContextKey)
            return;

        CancelNativeCompensationDraftCapabilityCheck();
        _compensationCapabilityContextKey = backendContextKey;

        var cancellationSource = new CancellationTokenSource();
        _compensationCapabilityCheck = cancellationSource;
        var generation = ++_compensationCapabilityGeneration;

        // Check an ordinary, origin-bound document before it can gain its first native scope. Once a
        // fresh result exists for this backend, retain it while a server save response is adopted or
        // the graph first exposes the scope; clearing it mid-interaction would unmount the editor.
        if (!canRetainCapability)
            _nativeCompensationDraftCapabilityEnabled = false;

        bool capabilityEnabled;
        try
        {
            capabilityEnabled = await NativeCompensationDraftCapability.IsEnabledForContextAsync(
                RemoteFeatureProvider, BackendApiClientProvider, backendContextKey, cancellationSource.Token);
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
            ReleaseNativeCompensationDraftCapabilityCheck(cancellationSource);
            return;
        }
        catch
        {
            capabilityEnabled = false;
        }

        var isCurrentCheck = generation == _compensationCapabilityGeneration &&
                             ReferenceEquals(_compensationCapabilityCheck, cancellationSource);
        ReleaseNativeCompensationDraftCapabilityCheck(cancellationSource);

        if (!isCurrentCheck || BackendApiClientProvider.ContextKey != backendContextKey)
            return;

        _nativeCompensationDraftCapabilityEnabled = capabilityEnabled && !_nativeCompensationDraftContextInvalidated;
    }

    private void CancelNativeCompensationDraftCapabilityCheck()
    {
        _compensationCapabilityCheck?.Cancel();
        _compensationCapabilityCheck?.Dispose();
        _compensationCapabilityCheck = null;
    }

    private void ReleaseNativeCompensationDraftCapabilityCheck(CancellationTokenSource cancellationSource)
    {
        if (ReferenceEquals(_compensationCapabilityCheck, cancellationSource))
            _compensationCapabilityCheck = null;

        cancellationSource.Dispose();
    }

    private void CancelNativeGraphAnalysisCheck()
    {
        _nativeGraphAnalysisGeneration++;
        _nativeGraphAnalysisCheck?.Cancel();
        _nativeGraphAnalysisCheck?.Dispose();
        _nativeGraphAnalysisCheck = null;
    }

    private void ReleaseNativeGraphAnalysisCheck(CancellationTokenSource cancellationSource)
    {
        if (ReferenceEquals(_nativeGraphAnalysisCheck, cancellationSource))
            _nativeGraphAnalysisCheck = null;

        cancellationSource.Dispose();
    }

    private void OnBackendContextChanged() => _ = InvokeAsync(async () =>
    {
        if (_nativeGraphAnalysis.RequiresNativeProtection)
        {
            _nativeCompensationDraftContextInvalidated = true;
            if (WorkflowEditor != null)
                WorkflowEditor.InvalidateWorkflowDefinitionOperations();

            _workflowDefinitionReloadVersion++;
            _nativeCompensationDraftReloadError = null;
        }

        _nativeCompensationDraftCapabilityEnabled = false;
        _compensationCapabilityContextKey = null;
        StateHasChanged();
        await RefreshNativeCompensationDraftCapabilityAsync();
        await RefreshNativeCompensationDraftGraphAnalysisAsync();
        StateHasChanged();
    });

    private async Task ReloadNativeCompensationDraftAsync()
    {
        if (_selectedWorkflowDefinition == null || string.IsNullOrWhiteSpace(_selectedWorkflowDefinition.DefinitionId))
        {
            _nativeCompensationDraftReloadError = "This draft has no saved workflow definition to reload.";
            return;
        }

        var expectedBackendContextKey = BackendApiClientProvider.ContextKey;
        _isReloadingNativeCompensationDraft = true;
        _nativeCompensationDraftReloadError = null;

        try
        {
            var definition = await WorkflowDefinitionService.FindByDefinitionIdAsync(
                _selectedWorkflowDefinition.DefinitionId, VersionOptions.Latest);

            if (BackendApiClientProvider.ContextKey != expectedBackendContextKey ||
                definition == null ||
                NativeCompensationDraftDocumentContext.GetBackendContextKey(definition) != expectedBackendContextKey)
            {
                _nativeCompensationDraftReloadError = "The workflow could not be reloaded from the current backend context. The local draft remains available if you return to its original context.";
                return;
            }

            if (WorkflowEditor != null)
                WorkflowEditor.InvalidateWorkflowDefinitionOperations();

            _workflowDefinition = definition;
            _selectedWorkflowDefinition = definition;
            _compensationDocumentContextKey = expectedBackendContextKey;
            _nativeCompensationDraftContextInvalidated = false;
            _workflowDefinitionReloadVersion++;
            await RefreshNativeCompensationDraftCapabilityAsync();
            await RefreshNativeCompensationDraftGraphAnalysisAsync(force: true);
        }
        catch (OperationCanceledException)
        {
            _nativeCompensationDraftReloadError = "The backend context changed while reloading. The local draft remains available if you return to its original context.";
        }
        catch (Exception)
        {
            _nativeCompensationDraftReloadError = "The workflow could not be reloaded from the current backend context. The local draft remains available if you return to its original context.";
        }
        finally
        {
            _isReloadingNativeCompensationDraft = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private string? GetCompensationDocumentContextKey(WorkflowDefinition? definition)
    {
        return NativeCompensationDraftDocumentContext.GetBackendContextKey(definition);
    }

    public void Dispose()
    {
        PermissionRefreshSignal.Raised -= OnBackendContextChanged;
        _compensationCapabilityGeneration++;
        _compensationCapabilityCheck?.Cancel();
        _compensationCapabilityCheck?.Dispose();
        _compensationCapabilityCheck = null;
        _nativeGraphAnalysisGeneration++;
        _nativeGraphAnalysisCheck?.Cancel();
        _nativeGraphAnalysisCheck?.Dispose();
        _nativeGraphAnalysisCheck = null;
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
        _compensationDocumentContextKey = GetCompensationDocumentContextKey(workflowDefinition);
        _nativeCompensationDraftContextInvalidated = _nativeGraphAnalysis.RequiresNativeProtection &&
                                                     _nativeCompensationDraftContextInvalidated &&
                                                     _compensationDocumentContextKey != BackendApiClientProvider.ContextKey;
        _nativeCompensationDraftCapabilityEnabled = false;
        _compensationCapabilityContextKey = null;

        if (workflowDefinition.IsLatest)
            _workflowDefinition = workflowDefinition;

        await RefreshNativeCompensationDraftCapabilityAsync();
        await RefreshNativeCompensationDraftGraphAnalysisAsync();

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
        if (IsReadOnly)
            return;

        if (WorkflowEditor != null)
            await WorkflowEditor.NotifyWorkflowChangedAsync(_isCodeViewTab);

        if (_isCodeViewTab && CodeView is not null)
            await CodeView.UpdateCodeViewFromEditorAsync(WorkflowDefinitionSerialized);

        if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();
    }

    private async Task OnWorkflowDefinitionUpdated()
    {
        var updatedWorkflowDefinition = WorkflowEditor.WorkflowDefinition!;
        var isDefinitionReplacement = !ReferenceEquals(_selectedWorkflowDefinition, updatedWorkflowDefinition);
        _workflowDefinition = updatedWorkflowDefinition;
        _selectedWorkflowDefinition = _workflowDefinition;
        _compensationDocumentContextKey = GetCompensationDocumentContextKey(_selectedWorkflowDefinition);
        if (isDefinitionReplacement)
            await RefreshNativeCompensationDraftCapabilityAsync(force: true);
        await RefreshNativeCompensationDraftGraphAnalysisAsync(force: isDefinitionReplacement);
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnWorkflowDefinitionReloaded()
    {
        _workflowDefinitionReloadVersion++;
        await OnWorkflowDefinitionUpdated();
    }

    private async Task OnApplyWorkflowDefinition(WorkflowDefinition deserialized)
    {
        if (IsReadOnly)
            return;

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
