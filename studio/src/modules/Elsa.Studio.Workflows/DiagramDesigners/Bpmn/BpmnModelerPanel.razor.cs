using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// What a BPMN workflow's properties pane shows while the enabled BPMN designer edits its document: whether the
/// document could be read, shown and saved, the unsaved state, the problems the server found in it, and the element
/// selected on the canvas.
/// </summary>
public partial class BpmnModelerPanel : IDisposable
{
    private BpmnModelerSession? _subscribedSession;

    /// <summary>The document the designer edits.</summary>
    [Parameter, EditorRequired] public BpmnModelerSession Session { get; set; } = null!;

    /// <summary>The workflow definition being edited, for input editors that read its variables.</summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>Invoked to save the workflow, which is the only thing that writes the document back.</summary>
    [Parameter] public Func<Task>? SaveRequested { get; set; }

    /// <summary>Invoked to reload the workflow and its document from the server, discarding unsaved edits.</summary>
    [Parameter] public Func<Task>? ReloadRequested { get; set; }

    /// <summary>The visible height of the pane, in pixels.</summary>
    [Parameter] public int VisiblePaneHeight { get; set; } = 300;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribedSession, Session))
            return;

        if (_subscribedSession != null)
            _subscribedSession.Changed -= OnSessionChanged;

        Session.Changed += OnSessionChanged;
        _subscribedSession = Session;
    }

    private void OnSessionChanged() => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_subscribedSession != null)
            _subscribedSession.Changed -= OnSessionChanged;
    }

    private async Task OnSaveClicked()
    {
        if (SaveRequested != null)
            await SaveRequested();
    }

    /// <summary>Selects the element a problem is about, so the panel shows it.</summary>
    private Task OnProblemElementClicked(string elementId) => Session.SelectAsync(elementId);

    private async Task OnReloadClicked()
    {
        if (ReloadRequested != null)
            await ReloadRequested();
    }

    private string DescribeLoadFailure(BpmnDocumentFailure failure) => failure.Reason switch
    {
        BpmnDocumentFailureReason.SourceStale => Localizer["This workflow was changed outside its BPMN document since it was imported, so the document no longer describes it. Import the BPMN file into this workflow again to edit it."],
        BpmnDocumentFailureReason.NotImportedFromBpmn => Localizer["This workflow has no BPMN document to edit: it was not imported from BPMN, or a later save removed the document."],
        BpmnDocumentFailureReason.MissingETag => Localizer["The server returned the BPMN document without an ETag, so a change could not be saved. If Studio runs on a different origin than the server, the server's CORS policy must expose the ETag header."],
        BpmnDocumentFailureReason.NotFound => Localizer["This workflow definition no longer exists."],
        _ => Localizer["The BPMN document could not be read: {0}", failure.Message]
    };

    private string DescribeSaveFailure(BpmnDocumentFailure failure) => failure.Reason switch
    {
        BpmnDocumentFailureReason.PreconditionFailed => Localizer["This workflow was changed since its BPMN document was read, by another save or another user, so the diagram changes were not saved. Reload to continue from the current version; unsaved diagram changes will be lost."],
        BpmnDocumentFailureReason.PreconditionRequired => Localizer["The server refused the save because it did not name the revision it replaces, so nothing was saved. Reload and make the change again."],
        BpmnDocumentFailureReason.BindingInvalid => Localizer["The server refused a binding in the document, so nothing was saved: {0}", failure.Message],
        BpmnDocumentFailureReason.CapabilityUnsupported => Localizer["The server cannot run this process, so nothing was saved: {0}", failure.Message],
        BpmnDocumentFailureReason.ConditionUnsupported when failure.ConditionRefusal is { } refusal =>
            Localizer["The process's sequence flows {0} carry expression conditions, which Elsa does not evaluate yet, so nothing was saved.", string.Join(", ", refusal.FlowIds.Select(id => $"'{id}'"))],
        BpmnDocumentFailureReason.NotFound => Localizer["This workflow definition no longer exists."],
        _ => Localizer["The diagram changes could not be saved: {0}", failure.Message]
    };
}
