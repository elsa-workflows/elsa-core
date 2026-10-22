using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// One BPMN workflow's document while a BPMN designer edits it: the XML revision the server last returned, what the
/// designer reported about it, and the save that writes the designer's document back through the XML <c>PUT</c>.
/// </summary>
/// <remarks>
/// <para>
/// The designer holds the working copy; this session holds what the server has. <see cref="Xml"/> changes only when the
/// document is read (<see cref="EnsureLoadedAsync"/>, <see cref="ReloadAsync"/>), and each read raises
/// <see cref="DocumentVersion"/>, which tells the canvas to load it. A save does not: the canvas already shows what was
/// saved, along with its undo history and any edit made while the save ran.
/// </para>
/// <para>
/// <b>Optimistic concurrency.</b> Every <c>PUT</c> carries, as <c>If-Match</c>, the <c>ETag</c> of the revision the edit
/// was made against. A refusal (<see cref="BpmnDocumentFailureReason.PreconditionFailed"/>) is reported, never retried.
/// </para>
/// <para>
/// <b>Lost content.</b> <see cref="SaveGate"/> holds the designer's load warnings that report content a save would drop.
/// The session does not ask the user; the editor does, before it calls <see cref="SaveAsync"/>, which refuses while an
/// acknowledgement is still owed.
/// </para>
/// </remarks>
public sealed class BpmnModelerSession(IBpmnXmlService bpmnXmlService, string definitionId)
{
    private IBpmnModelerCanvas? _canvas;
    private Task? _loadTask;
    private Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>>? _saveTask;
    private string? _eTag;
    private bool _canvasDirty;
    private bool _unsavedExport;
    private long _changeSequence;

    /// <summary>The workflow definition whose document this is.</summary>
    public string DefinitionId { get; } = definitionId;

    /// <summary>
    /// Raised whenever anything this session reports changes, so the components showing it can re-render. Raised from
    /// whichever context made the change; handlers are expected to marshal with <c>InvokeAsync</c>.
    /// </summary>
    public event Action? Changed;

    /// <summary>The document as the server last returned it, or <see langword="null"/> until a read succeeds.</summary>
    public string? Xml { get; private set; }

    /// <summary>Raised by every read that replaces <see cref="Xml"/>; the canvas loads the document again when it changes.</summary>
    public int DocumentVersion { get; private set; }

    /// <summary>Whether a read is in flight.</summary>
    public bool IsLoading => _loadTask is { IsCompleted: false };

    /// <summary>Whether a save is in flight.</summary>
    public bool IsSaving => _saveTask is { IsCompleted: false };

    /// <summary>Why the last read failed, or <see langword="null"/> when it did not.</summary>
    public BpmnDocumentFailure? LoadFailure { get; private set; }

    /// <summary>Why the canvas could not show the document, or <see langword="null"/> when it could or has not tried.</summary>
    public string? CanvasLoadError { get; private set; }

    /// <summary>Why the last save failed, or <see langword="null"/> when it did not.</summary>
    public BpmnDocumentFailure? SaveFailure { get; private set; }

    /// <summary>
    /// Whether the last save was written, but its new revision could not be confirmed: the response carried no
    /// <c>ETag</c>, and the document read back was not the one sent. The next save is refused as stale until a reload.
    /// </summary>
    public bool SavedButRevisionUnknown { get; private set; }

    /// <summary>The foreign content the server reported as dropped by the last successful save.</summary>
    public IReadOnlyList<BpmnForeignContentModel> DroppedForeignContent { get; private set; } = [];

    /// <summary>The designer's load warnings for the current document, and whether the user accepted losing what they report.</summary>
    public BpmnSaveGate SaveGate { get; } = new();

    /// <summary>The designer's edits since the document was loaded or last saved, or a save that failed after they were exported.</summary>
    public bool IsDirty => _canvasDirty || _unsavedExport;

    /// <summary>Whether the designer has an edit to undo.</summary>
    public bool CanUndo { get; private set; }

    /// <summary>Whether the designer has an undone edit to redo.</summary>
    public bool CanRedo { get; private set; }

    /// <summary>The element selected on the canvas, or <see langword="null"/> when none is.</summary>
    public BpmnDesignerSelectedElement? SelectedElement { get; private set; }

    /// <summary>Whether a canvas is attached, so a save has a document to send.</summary>
    public bool HasCanvas => _canvas != null;

    /// <summary>Reads the document, unless a read has already started.</summary>
    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => _loadTask ?? ReloadAsync(cancellationToken);

    /// <summary>Reads the document again; the canvas then loads it, discarding the designer's unsaved edits.</summary>
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        _loadTask = LoadAsync(cancellationToken);
        Changed?.Invoke();
        return _loadTask;
    }

    /// <summary>Connects the canvas that edits this document. Only one canvas edits a session at a time.</summary>
    internal void AttachCanvas(IBpmnModelerCanvas canvas)
    {
        _canvas = canvas;
        Changed?.Invoke();
    }

    /// <summary>Disconnects <paramref name="canvas"/>, if it is the one attached.</summary>
    internal void DetachCanvas(IBpmnModelerCanvas canvas)
    {
        if (!ReferenceEquals(_canvas, canvas))
            return;

        _canvas = null;
        _canvasDirty = false;
        CanUndo = CanRedo = false;
        SelectedElement = null;
        Changed?.Invoke();
    }

    /// <summary>Records what the designer's <c>load</c> of <see cref="Xml"/> returned.</summary>
    internal void CanvasLoaded(BpmnDesignerLoadResult result)
    {
        CanvasLoadError = result.Imported ? null : result.Error ?? "The designer could not show the document.";

        if (result.Imported)
        {
            SaveGate.Loaded(result.Warnings);
            _canvasDirty = _unsavedExport = false;
            CanUndo = CanRedo = false;
            SelectedElement = null;
        }

        Changed?.Invoke();
    }

    /// <summary>Records the designer's <c>change</c> event.</summary>
    internal void CanvasChanged(BpmnDesignerChange change)
    {
        _changeSequence++;
        _canvasDirty = change.IsDirty;
        CanUndo = change.CanUndo;
        CanRedo = change.CanRedo;

        if (change.IsDirty)
            SaveFailure = null;

        Changed?.Invoke();
    }

    /// <summary>Records the designer's <c>selection</c> event.</summary>
    internal void CanvasSelectionChanged(BpmnDesignerSelection selection)
    {
        SelectedElement = selection.Primary;
        Changed?.Invoke();
    }

    /// <summary>Undoes the designer's last edit.</summary>
    public Task UndoAsync() => _canvas?.UndoAsync() ?? Task.CompletedTask;

    /// <summary>Redoes the designer's last undone edit.</summary>
    public Task RedoAsync() => _canvas?.RedoAsync() ?? Task.CompletedTask;

    /// <summary>
    /// Sends the designer's document through the XML <c>PUT</c>, against the revision it was loaded or last saved as.
    /// </summary>
    /// <remarks>
    /// Single-flight: a call made while a save is in flight returns that save. The designer counts its document as saved
    /// the moment it exports it; until the <c>PUT</c> succeeds, this session keeps the document dirty, so a failed save
    /// still shows unsaved changes.
    /// </remarks>
    /// <returns>The server's result on success, or why the document was not saved.</returns>
    public Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (_saveTask is { IsCompleted: false })
            return _saveTask;

        if (_canvas == null || Xml == null || CanvasLoadError != null)
            return Task.FromResult(Refuse(new(BpmnDocumentFailureReason.Unknown, "The BPMN document is not open in a designer, so there is nothing to save.")));

        if (_eTag == null)
            return Task.FromResult(Refuse(new(BpmnDocumentFailureReason.PreconditionFailed, "The revision of the last save could not be confirmed, so nothing was saved. Reload to continue from the current version.")));

        if (SaveGate.RequiresAcknowledgement)
            return Task.FromResult(Refuse(new(BpmnDocumentFailureReason.Unknown, "The content the designer cannot keep has not been acknowledged, so nothing was saved.")));

        SaveFailure = null;
        SavedButRevisionUnknown = false;
        _saveTask = SaveCoreAsync(_canvas, _eTag, cancellationToken);
        Changed?.Invoke();
        return _saveTask;
    }

    /// <summary>
    /// Called after an ordinary workflow-definition save, which may advance the definition's revision without changing
    /// its document. Re-reads the document and, if it is still the one this session holds, adopts the new <c>ETag</c> so
    /// the next <see cref="SaveAsync"/> is not refused for a change nobody made. A different document replaces this one
    /// when the designer has nothing unsaved; otherwise it is a genuine conflict, reported as a stale <c>PUT</c> would be,
    /// without sending anything.
    /// </summary>
    /// <returns><see langword="true"/> if <see cref="SaveAsync"/> may proceed; otherwise the refusal is in <see cref="SaveFailure"/>.</returns>
    public async Task<bool> RefreshRevisionAfterOrdinarySaveAsync(CancellationToken cancellationToken = default)
    {
        if (_saveTask is { IsCompleted: false })
            await _saveTask;

        if (Xml == null)
            return false;

        var result = await TryAsync(() => bpmnXmlService.GetXmlAsync(DefinitionId, cancellationToken));

        if (!result.IsSuccess)
        {
            Refuse(result.Failure!);
            return false;
        }

        if (result.Success!.Xml != Xml && !IsDirty)
        {
            Adopt(result.Success);
            Changed?.Invoke();
            return true;
        }

        if (result.Success.Xml != Xml)
        {
            Refuse(new(
                BpmnDocumentFailureReason.PreconditionFailed,
                "This workflow was changed since its BPMN document was read, by another save or another user, so the diagram changes were not saved."));
            return false;
        }

        _eTag = result.Success.ETag;
        SaveFailure = null;
        return true;
    }

    private async Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> SaveCoreAsync(IBpmnModelerCanvas canvas, string eTag, CancellationToken cancellationToken)
    {
        try
        {
            var changeSequence = _changeSequence;
            string xml;

            try
            {
                xml = await canvas.SaveAsync();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Refuse(new(BpmnDocumentFailureReason.Unknown, $"The designer could not export the document: {exception.Message}"));
            }

            // The designer now counts the exported document as saved. An edit made while it exported raised a change
            // event of its own, which already says whether the designer is still dirty.
            if (_changeSequence == changeSequence)
                _canvasDirty = false;

            _unsavedExport = true;

            var result = await TryAsync(() => bpmnXmlService.PutXmlAsync(DefinitionId, xml, eTag, cancellationToken));

            if (!result.IsSuccess)
                return Refuse(result.Failure!);

            _unsavedExport = false;
            Xml = xml;
            DroppedForeignContent = result.Success!.DroppedForeignContent;
            _eTag = result.Success.ETag ?? await ConfirmRevisionAsync(xml, cancellationToken);
            SavedButRevisionUnknown = _eTag == null;
            return result;
        }
        finally
        {
            _saveTask = null;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Reads the revision a save without an <c>ETag</c> in its response produced, and adopts its <c>ETag</c> only if the
    /// stored document is the one just sent. Otherwise someone else wrote in between, and the next save must be refused.
    /// </summary>
    private async Task<string?> ConfirmRevisionAsync(string xml, CancellationToken cancellationToken)
    {
        var result = await TryAsync(() => bpmnXmlService.GetXmlAsync(DefinitionId, cancellationToken));
        return result.Success is { } revision && revision.Xml == xml ? revision.ETag : null;
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await TryAsync(() => bpmnXmlService.GetXmlAsync(DefinitionId, cancellationToken));

        LoadFailure = result.Failure;
        SaveFailure = null;
        SavedButRevisionUnknown = false;
        DroppedForeignContent = [];

        if (result.Success is { } revision)
            Adopt(revision);

        Changed?.Invoke();
    }

    /// <summary>Makes <paramref name="revision"/> the document the canvas shows, which makes it load it.</summary>
    private void Adopt(BpmnXmlRevision revision)
    {
        Xml = revision.Xml;
        _eTag = revision.ETag;
        CanvasLoadError = null;
        DocumentVersion++;
    }

    /// <summary>
    /// Reports a request that never got an answer — the server unreachable, the connection dropped — as a failure the
    /// editor shows, rather than an exception that would take the whole editor down with it.
    /// </summary>
    private static async Task<Result<T, BpmnDocumentFailure>> TryAsync<T>(Func<Task<Result<T, BpmnDocumentFailure>>> request)
    {
        try
        {
            return await request();
        }
        catch (HttpRequestException exception)
        {
            return new(new BpmnDocumentFailure(BpmnDocumentFailureReason.Unknown, exception.Message));
        }
    }

    private Result<BpmnXmlSaveResult, BpmnDocumentFailure> Refuse(BpmnDocumentFailure failure)
    {
        SaveFailure = failure;
        return new(failure);
    }
}
