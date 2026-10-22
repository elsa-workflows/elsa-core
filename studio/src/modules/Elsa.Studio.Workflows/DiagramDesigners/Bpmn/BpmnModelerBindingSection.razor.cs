using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// The "Performed by" section for the element selected in the enabled BPMN designer: the activity picker and that
/// activity's own input editors, read from and written to the element's <c>elsa:activityBinding</c> through the
/// designer's <c>getBinding</c> and <c>setBinding</c>.
/// </summary>
/// <remarks>
/// Every edit is one undoable edit in the designer's document, saved with the rest of the diagram. The section never
/// parses or patches the XML. It reads the binding again after every change the designer reports, and keeps the draft
/// being edited when the binding read back is the one the draft wrote, so typing in an input editor is not interrupted.
/// </remarks>
public partial class BpmnModelerBindingSection : IDisposable
{
    private readonly ExpressionDescriptorProvider _expressionDescriptorProvider = new();
    private BpmnModelerSession? _subscribedSession;
    private (string? ElementId, int DocumentVersion, long ChangeSequence)? _readKey;
    private long _readNumber;
    private bool _isInitialized;
    private string? _elementId;
    private BpmnDesignerBinding? _binding;
    private string? _bindingError;
    private string? _refusal;
    private BpmnActivityBindingDraft? _draft;
    private int _draftGeneration;

    /// <summary>The document the designer edits.</summary>
    [Parameter, EditorRequired] public BpmnModelerSession Session { get; set; } = null!;

    /// <summary>The element selected on the canvas.</summary>
    [Parameter, EditorRequired] public BpmnDesignerSelectedElement Element { get; set; } = null!;

    /// <summary>The workflow definition being edited, for input editors that read its variables.</summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IExpressionService ExpressionService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private string ElementLabel => string.IsNullOrWhiteSpace(Element.Name) ? Element.Id : Element.Name;
    private string? BoundActivityName => _draft != null ? DescribeDescriptor(_draft.Descriptor) : _binding?.ActivityType;

    /// <summary>
    /// Whether the element is a task, the only kind of element a binding can be set on. A task that sends or waits for
    /// a message is bound automatically; the designer refuses to set a binding on it, and the section says so.
    /// </summary>
    private bool IsTask => Element.Type.EndsWith("Task", StringComparison.Ordinal);

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
        if (!ReferenceEquals(_subscribedSession, Session))
        {
            if (_subscribedSession != null)
                _subscribedSession.Changed -= OnSessionChanged;

            Session.Changed += OnSessionChanged;
            _subscribedSession = Session;
        }

        await SyncAsync();
    }

    private void OnSessionChanged() => _ = InvokeAsync(async () =>
    {
        await SyncAsync();
        StateHasChanged();
    });

    /// <summary>
    /// Reads the element's binding again when the element, the document or the designer's edits have moved on since the
    /// last read. A draft whose binding is the one read back is kept, with its editors; any other binding replaces it.
    /// </summary>
    private async Task SyncAsync()
    {
        if (!_isInitialized)
            return;

        var key = (Element.Id, Session.DocumentVersion, Session.ChangeSequence);

        if (_readKey == key)
            return;

        _readKey = key;
        var isSameElement = _elementId == Element.Id;
        _elementId = Element.Id;

        if (!isSameElement)
            _refusal = null;

        if (!IsTask)
        {
            Reset(null);
            return;
        }

        // Reads overlap when the designer reports changes in quick succession; only the latest one may land.
        var readNumber = ++_readNumber;
        var binding = await Session.GetBindingAsync(Element.Id);

        if (readNumber != _readNumber)
            return;

        if (isSameElement && _draft != null && BpmnDesignerBindings.AreEquivalent(binding, BpmnDesignerBindings.From(_draft)))
        {
            _binding = binding;
            return;
        }

        Reset(binding);
    }

    private void Reset(BpmnDesignerBinding? binding)
    {
        _binding = binding;
        _bindingError = null;
        SetDraft(null);

        if (binding == null)
            return;

        BpmnActivityBinding read;

        try
        {
            read = BpmnDesignerBindings.Read(binding);
        }
        catch (BpmnActivityBindingFormatException exception)
        {
            _bindingError = exception.Message;
            return;
        }

        if (LatestDescriptorOf(read.ActivityType) is { } descriptor)
            SetDraft(BpmnActivityBindingDraft.FromBinding(read, descriptor, Element.Id));
    }

    /// <summary>A binding names no version, and elsa-core builds the latest one it has, so that is the one to edit.</summary>
    private ActivityDescriptor? LatestDescriptorOf(string activityType) => ActivityRegistry.FindAll(activityType).MaxBy(descriptor => descriptor.Version);

    private void SetDraft(BpmnActivityBindingDraft? draft)
    {
        _draft = draft;
        _draftGeneration++;
    }

    private async Task OnPickActivityClicked()
    {
        var parameters = new DialogParameters<StateMachineActivityPickerDialog>
        {
            { x => x.IsReplacing, BoundActivityName != null },
            { x => x.DescriptorFilter, BpmnPerformedByPanel.IsLeafWork },
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
        if (result is not { Canceled: false, Data: ActivityDescriptor descriptor })
            return;

        _bindingError = null;
        SetDraft(BpmnActivityBindingDraft.Create(LatestDescriptorOf(descriptor.TypeName) ?? descriptor, Element.Id));
        await ApplyDraftAsync();
    }

    private Task OnDraftUpdatedAsync(JsonObject activity) => ApplyDraftAsync();

    /// <summary>Writes the draft into the designer's document. A refusal is shown, and the document is left as it was.</summary>
    private async Task ApplyDraftAsync()
    {
        if (_draft == null || _elementId == null)
            return;

        var result = await Session.SetBindingAsync(_elementId, BpmnDesignerBindings.From(_draft));
        _refusal = result.Applied ? null : result.Reason ?? string.Empty;
        StateHasChanged();
    }

    private string DescribeDescriptor(ActivityDescriptor descriptor) => $"{Localizer[descriptor.DisplayName ?? descriptor.Name]} ({descriptor.TypeName})";

    private string DescribeRefusal(string reason) => reason switch
    {
        BpmnDesignerSetBindingRefusals.NotBindable => Localizer["'{0}' sends or waits for a message, so Elsa binds it automatically and its activity cannot be chosen here.", ElementLabel],
        BpmnDesignerSetBindingRefusals.UnknownElement => Localizer["'{0}' is no longer in the diagram.", ElementLabel],
        _ => Localizer["The designer did not apply the binding: {0}", reason]
    };

    /// <inheritdoc />
    public void Dispose()
    {
        if (_subscribedSession != null)
            _subscribedSession.Changed -= OnSessionChanged;
    }
}
