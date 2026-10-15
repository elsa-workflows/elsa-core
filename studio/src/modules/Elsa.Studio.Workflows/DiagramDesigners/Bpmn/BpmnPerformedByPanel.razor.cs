using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// The "Performed by" section a BPMN-imported workflow's properties pane shows in place of the activity properties
/// panel: for a task whose activity is authored on it, the activity picker and that activity's own input editors,
/// written into the task's <c>elsa:activityBinding</c> in the workflow's BPMN document; for an element the document
/// binds automatically, what it is bound to, read-only.
/// </summary>
/// <remarks>
/// Every edit goes into <see cref="Session"/>'s working copy of the document and nowhere else. The activity the input
/// editors edit is a draft of the panel's own (<see cref="BpmnActivityBindingDraft"/>), never a node of the workflow's
/// activity graph, so nothing here can change the graph an ordinary workflow-definition save sends.
/// </remarks>
public partial class BpmnPerformedByPanel : IDisposable
{
    private readonly ExpressionDescriptorProvider _expressionDescriptorProvider = new();
    private bool _isInitialized;
    private string? _elementId;
    private JsonObject? _element;
    private BpmnActivityBinding? _binding;
    private string? _bindingError;
    private BpmnActivityBindingDraft? _draft;
    private int _draftGeneration;
    private bool _isSaving;
    private BpmnDocumentSession? _subscribedSession;

    /// <summary>The workflow definition being edited, for input editors that read its variables.</summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>The workflow's root <c>Elsa.BpmnProcess</c> activity, to name what an element is bound to.</summary>
    [Parameter] public JsonObject? RootActivity { get; set; }

    /// <summary>The BPMN element selected on the canvas, or <see langword="null"/> when none is.</summary>
    [Parameter] public BpmnElementSelection? Selection { get; set; }

    /// <summary>The workflow's BPMN document, which every binding edit is written into.</summary>
    [Parameter, EditorRequired] public BpmnDocumentSession Session { get; set; } = null!;

    /// <summary>Invoked after the working copy of the document changed, so the editor can show it is unsaved.</summary>
    [Parameter] public Func<Task>? DocumentChanged { get; set; }

    /// <summary>Invoked to save the workflow, which is the only thing that writes the document back.</summary>
    [Parameter] public Func<Task>? SaveRequested { get; set; }

    /// <summary>Invoked to reload the workflow and its document from the server, discarding unsaved edits.</summary>
    [Parameter] public Func<Task>? ReloadRequested { get; set; }

    /// <summary>The visible height of the pane, in pixels.</summary>
    [Parameter] public int VisiblePaneHeight { get; set; } = 300;

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IExpressionService ExpressionService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private string ElementLabel => string.IsNullOrWhiteSpace(Selection?.Name) ? Selection?.ElementId ?? string.Empty : Selection.Name;
    /// <summary>The activity the workflow's graph binds the selected element to, as the server last delivered it.</summary>
    private JsonObject? GraphBoundActivity => Selection?.ActivityId is { } activityId ? RootActivity?.FindActivity(activityId) : null;
    private string? BoundActivityName => _draft != null ? DescribeDescriptor(_draft.Descriptor) : _binding?.ActivityType;

    /// <summary>Whether a binding edit could be saved at all; while it could not, none is offered.</summary>
    private bool CanEdit => Session.Document != null;

    /// <summary>
    /// Whether the selected element's absence from the document (<see cref="_element"/> is <see langword="null"/>) is
    /// explained by it living in a nested scope — a subprocess, transaction, or event subprocess — whose body the
    /// document never carries (elsa-workflows/elsa-core#8076), rather than some other mismatch between the canvas and
    /// the document (a stale or unknown element id).
    /// </summary>
    private bool ElementIsInSubprocess =>
        Selection != null
        && Session.Document != null
        && !BpmnDefinitionsDocument.HasTopLevelProcess(Session.Document, Selection.ScopeId);

    /// <summary>
    /// Leaf work: an activity that performs one unit of work itself. A container or a composite schedules other
    /// activities, which a single BPMN task cannot hold.
    /// </summary>
    internal static bool IsLeafWork(ActivityDescriptor descriptor) =>
        descriptor.IsBrowsable
        && !descriptor.IsContainer
        && descriptor.TypeName != BpmnProcessConstants.ActivityTypeName
        && descriptor.Ports.All(port => port.Type != PortType.Embedded);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _expressionDescriptorProvider.AddRange(await ExpressionService.ListDescriptorsAsync());
        await ActivityRegistry.EnsureLoadedAsync();
        _isInitialized = true;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        SubscribeToSession();
        await Session.EnsureLoadedAsync();
        SyncDraft();
    }

    /// <summary>
    /// Keeps the panel subscribed to <see cref="Session"/>'s <see cref="BpmnDocumentSession.Changed"/> event, so it
    /// re-renders while a save is in flight rather than only before and after — an editor around it that shows progress
    /// with a single <c>StateHasChanged</c> before and after the whole save would otherwise never render the window
    /// where <see cref="BpmnDocumentSession.IsSaving"/> is <see langword="true"/>.
    /// </summary>
    private void SubscribeToSession()
    {
        if (ReferenceEquals(_subscribedSession, Session))
            return;

        if (_subscribedSession != null)
            _subscribedSession.Changed -= OnSessionChanged;

        Session.Changed += OnSessionChanged;
        _subscribedSession = Session;
    }

    private void OnSessionChanged() => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_subscribedSession != null)
            _subscribedSession.Changed -= OnSessionChanged;
    }

    /// <summary>
    /// Re-reads the selected task's binding whenever the task, or the working copy it lives in, is a different one — a
    /// new selection, a reload, a discard — and keeps the draft otherwise, so an edit in progress keeps its editors.
    /// </summary>
    private void SyncDraft()
    {
        var elementId = Selection is { IsUnboundTask: true } ? Selection.ElementId : null;
        var element = elementId == null || !_isInitialized ? null : Session.FindElement(elementId);

        if (elementId == _elementId && ReferenceEquals(element, _element))
            return;

        _elementId = elementId;
        _element = element;
        _binding = null;
        _bindingError = null;
        SetDraft(null);

        if (element == null || BpmnActivityBindingFormat.Find(element) is not { } bindingElement)
            return;

        try
        {
            _binding = BpmnActivityBindingFormat.Read(bindingElement);
        }
        catch (BpmnActivityBindingFormatException exception)
        {
            _bindingError = exception.Message;
            return;
        }

        if (LatestDescriptorOf(_binding.ActivityType) is { } descriptor)
            SetDraft(BpmnActivityBindingDraft.FromBinding(_binding, descriptor, DraftActivityId()));
    }

    /// <summary>A binding names no version, and elsa-core builds the latest one it has, so that is the one to edit.</summary>
    private ActivityDescriptor? LatestDescriptorOf(string activityType) => ActivityRegistry.FindAll(activityType).MaxBy(descriptor => descriptor.Version);

    private void SetDraft(BpmnActivityBindingDraft? draft)
    {
        _draft = draft;
        _draftGeneration++;
    }

    /// <summary>The id the input editors key the draft on: the bound activity's own, once there is one.</summary>
    private string DraftActivityId() => Selection!.ActivityId ?? Selection.ElementId;

    private async Task OnPickActivityClicked()
    {
        var parameters = new DialogParameters<StateMachineActivityPickerDialog>
        {
            { x => x.IsReplacing, BoundActivityName != null },
            { x => x.DescriptorFilter, IsLeafWork },
            { x => x.ContextLabel, Localizer["PERFORMED BY"].Value },
            { x => x.ContextHint, Localizer["Runs when the process reaches '{0}'", ElementLabel].Value }
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large
        };

        var dialog = await DialogService.ShowAsync<StateMachineActivityPickerDialog>(Localizer["Choose the activity that performs this task"], parameters, options);
        var result = await dialog.Result;

        // Nothing changes until the picker returns an explicit choice: cancelling keeps the existing binding as it is.
        if (result is not { Canceled: false, Data: ActivityDescriptor descriptor } || _element == null)
            return;

        _binding = null;
        _bindingError = null;
        SetDraft(BpmnActivityBindingDraft.Create(LatestDescriptorOf(descriptor.TypeName) ?? descriptor, DraftActivityId()));
        await ApplyDraftAsync();
    }

    private Task OnDraftUpdatedAsync(JsonObject activity) => ApplyDraftAsync();

    private async Task ApplyDraftAsync()
    {
        // The UI already disables editing while Session.IsSaving; this is only the backstop for an edit — a debounced
        // input, a pending dialog result — that reaches here anyway. It is simply dropped: the working copy did not
        // change, and the section already shows the saving state instead of the editors that produced it.
        if (!Session.SetBinding(_elementId!, _draft!.ToBinding()))
            return;

        if (DocumentChanged != null)
            await DocumentChanged();

        StateHasChanged();
    }

    private async Task OnSaveClicked()
    {
        _isSaving = true;

        try
        {
            if (SaveRequested != null)
                await SaveRequested();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task OnDiscardClicked()
    {
        Session.Discard();
        SyncDraft();

        if (DocumentChanged != null)
            await DocumentChanged();
    }

    private async Task OnReloadClicked()
    {
        if (ReloadRequested != null)
            await ReloadRequested();
    }

    private string? DescribeActivity(JsonObject? activity)
    {
        if (activity == null)
            return null;

        var typeName = activity.GetTypeName();
        var name = activity.GetName();

        if (!string.IsNullOrWhiteSpace(name))
            return $"{name} ({typeName})";

        return ActivityRegistry.Find(typeName, activity.GetVersion()) is { } descriptor ? DescribeDescriptor(descriptor) : typeName;
    }

    private string DescribeDescriptor(ActivityDescriptor descriptor) => $"{Localizer[descriptor.DisplayName ?? descriptor.Name]} ({descriptor.TypeName})";

    private string DescribeLoadFailure(BpmnDocumentFailure failure) => failure.Reason switch
    {
        BpmnDocumentFailureReason.SourceStale => Localizer["This workflow was changed outside its BPMN document since it was imported, so the document no longer describes it. Import the BPMN file into this workflow again to edit its bindings."],
        BpmnDocumentFailureReason.NotImportedFromBpmn => Localizer["This workflow has no BPMN document to edit: it was not imported from BPMN, or a later save removed the document."],
        BpmnDocumentFailureReason.MissingETag => Localizer["The server returned the BPMN document without an ETag, so a binding change could not be saved. If Studio runs on a different origin than the server, the server's CORS policy must expose the ETag header."],
        BpmnDocumentFailureReason.NotFound => Localizer["This workflow definition no longer exists."],
        _ => Localizer["The BPMN document could not be read: {0}", failure.Message]
    };

    private string DescribeSaveFailure(BpmnDocumentFailure failure) => failure.Reason switch
    {
        BpmnDocumentFailureReason.PreconditionFailed => Localizer["This workflow was changed since its BPMN document was read, by another save or another user, so the binding changes were not saved. Reload to continue from the current version; unsaved binding changes will be lost."],
        BpmnDocumentFailureReason.PreconditionRequired => Localizer["The server refused the save because it did not name the revision it replaces, so nothing was saved. Reload and make the change again."],
        BpmnDocumentFailureReason.BindingInvalid => Localizer["The server refused the binding of {0}, so nothing was saved: {1}", string.Join(", ", Session.EditedElementIds.Select(id => $"'{id}'")), failure.Message],
        BpmnDocumentFailureReason.CapabilityUnsupported => Localizer["The server cannot run this process, so nothing was saved: {0}", failure.Message],
        BpmnDocumentFailureReason.NotFound => Localizer["This workflow definition no longer exists."],
        _ => Localizer["The binding changes could not be saved: {0}", failure.Message]
    };
}
