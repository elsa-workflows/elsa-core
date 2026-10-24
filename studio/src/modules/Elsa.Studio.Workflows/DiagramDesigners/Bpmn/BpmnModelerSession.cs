using System.Net;
using System.Text;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
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
/// <para>
/// <b>Problems.</b> Created with an <see cref="IBpmnInterchangeService"/>, the session has the server analyze each
/// document the canvas loads and each one a save sends, and reports what is wrong with it in <see cref="Problems"/>,
/// which it also marks on the canvas. The analysis checks the process the definition was imported from, the one a save
/// binds. A failed request, as opposed to a refused document, reports no problems. The designer's working copy is not
/// analyzed between saves: exporting it would mark it saved.
/// </para>
/// </remarks>
public sealed class BpmnModelerSession(IBpmnXmlService bpmnXmlService, string definitionId)
{
    /// <summary>
    /// The reason <see cref="SetBindingAsync"/> gives for not applying a binding while the designer does not show the
    /// latest document read: the edit would be made in a document that is about to be replaced.
    /// </summary>
    public const string DocumentNotReadyRefusal = "document-not-ready";

    private IBpmnModelerCanvas? _canvas;
    private Task? _loadTask;
    private Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>>? _saveTask;
    private string? _eTag;
    private bool _canvasDirty;
    private bool _unsavedExport;
    private long _changeSequence;
    private int _canvasDocumentVersion;
    private readonly IBpmnInterchangeService? _analyzer;
    private long _analysisSequence;
    private Task _analysis = Task.CompletedTask;

    /// <summary>
    /// Creates a session that also has the server analyze its documents, so <see cref="Problems"/> reports what is wrong
    /// with them.
    /// </summary>
    /// <param name="bpmnXmlService">Reads and saves the document.</param>
    /// <param name="analyzer">Analyzes each document the canvas loads and each one a refused save sends.</param>
    /// <param name="definitionId">The workflow definition whose document this is.</param>
    /// <param name="processId">
    /// The process the definition was imported from, which a save of a document declaring more than one process binds;
    /// the analysis checks that process too. <see langword="null"/> when the definition does not record one.
    /// </param>
    public BpmnModelerSession(IBpmnXmlService bpmnXmlService, IBpmnInterchangeService analyzer, string definitionId, string? processId = null) : this(bpmnXmlService, definitionId)
    {
        _analyzer = analyzer;
        ProcessId = processId;
    }

    /// <summary>
    /// The process the definition was imported from, which the analysis checks; <see langword="null"/> when the
    /// definition does not record one. An import that binds another process of the same document changes it.
    /// </summary>
    public string? ProcessId { get; internal set; }

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

    /// <summary>
    /// What the server reported wrong with the document the canvas last loaded or a save last sent: why its import would
    /// be refused, and content its read degrades or drops. Empty for a session created without an analyzer.
    /// </summary>
    public IReadOnlyList<BpmnDocumentProblem> Problems { get; private set; } = [];

    /// <summary>The analysis behind <see cref="Problems"/> that is in flight, or a completed task.</summary>
    internal Task Analysis => _analysis;

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

    /// <summary>
    /// Whether the designer shows the latest document read, so an edit made in it now is kept and saved. It is not while
    /// a read is in flight or until the canvas has loaded what a read returned, since loading it discards the designer's
    /// edits.
    /// </summary>
    public bool IsDocumentReady => _canvas != null && Xml != null && CanvasLoadError == null && !IsLoading && _canvasDocumentVersion == DocumentVersion;

    /// <summary>Counts the designer's <c>change</c> events, so a reader of the document can tell it may have changed.</summary>
    public long ChangeSequence => _changeSequence;

    /// <summary>The element selected on the canvas, or <see langword="null"/> when none is.</summary>
    public BpmnDesignerSelectedElement? SelectedElement { get; private set; }

    /// <summary>Whether a canvas is attached, so a save has a document to send.</summary>
    public bool HasCanvas => _canvas != null;

    /// <summary>Reads the document, unless a read has already started.</summary>
    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => _loadTask ?? ReloadAsync(cancellationToken);

    /// <summary>
    /// Reads the document again; the canvas then loads it, discarding the designer's unsaved edits. A save in flight
    /// finishes first, so the read cannot return the document from before it and hand that to the canvas as current, and
    /// a read already in flight finishes first too, so the latest read is the one that lands and <see cref="IsLoading"/>
    /// stays set until it has.
    /// </summary>
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        _loadTask = LoadAfterAsync(_loadTask, _saveTask, cancellationToken);
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

    /// <summary>Records what the designer's <c>load</c> of <see cref="Xml"/>, as of <paramref name="documentVersion"/>, returned.</summary>
    internal void CanvasLoaded(BpmnDesignerLoadResult result, int documentVersion)
    {
        CanvasLoadError = result.Imported ? null : result.Error ?? "The designer could not show the document.";

        if (result.Imported)
        {
            _canvasDocumentVersion = documentVersion;
            SaveGate.Loaded(result.Warnings);
            _canvasDirty = _unsavedExport = false;
            CanUndo = CanRedo = false;
            SelectedElement = null;
            if (Xml is { } xml)
            {
                Analyze(xml);
            }
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

    /// <summary>Selects the element with <paramref name="elementId"/> on the canvas. Returns whether the designer has it.</summary>
    public Task<bool> SelectAsync(string elementId) =>
        _canvas != null && CanvasLoadError == null ? _canvas.SelectAsync(elementId) : Task.FromResult(false);

    /// <summary>Undoes the designer's last edit.</summary>
    public Task UndoAsync() => _canvas?.UndoAsync() ?? Task.CompletedTask;

    /// <summary>Redoes the designer's last undone edit.</summary>
    public Task RedoAsync() => _canvas?.RedoAsync() ?? Task.CompletedTask;

    /// <summary>
    /// The <c>elsa:activityBinding</c> of the element with <paramref name="elementId"/> in the designer's document, or
    /// <see langword="null"/> when it has none or no designer shows the document.
    /// </summary>
    public Task<BpmnDesignerBinding?> GetBindingAsync(string elementId) =>
        _canvas != null && CanvasLoadError == null ? _canvas.GetBindingAsync(elementId) : Task.FromResult<BpmnDesignerBinding?>(null);

    /// <summary>
    /// Makes <paramref name="binding"/> the binding of the element with <paramref name="elementId"/> in the designer's
    /// document, as one undoable edit. Like every designer edit, it is sent with the next <see cref="SaveAsync"/>. While
    /// the document is not <see cref="IsDocumentReady"/>, it is refused with <see cref="DocumentNotReadyRefusal"/>.
    /// </summary>
    public Task<BpmnDesignerSetBindingResult> SetBindingAsync(string elementId, BpmnDesignerBinding binding) =>
        IsDocumentReady
            ? _canvas!.SetBindingAsync(elementId, binding)
            : Task.FromResult(new BpmnDesignerSetBindingResult(false, DocumentNotReadyRefusal));

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

        // A read replaces the designer's document when the canvas has loaded it, so until then what this save would send
        // is about to be discarded, and the revision it would be sent against is no longer the one it was edited from.
        if (IsLoading || _canvasDocumentVersion != DocumentVersion)
            return Task.FromResult(Refuse(new(BpmnDocumentFailureReason.Unknown, "The BPMN document is being read again, so nothing was saved.")));

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
        await WaitQuietlyAsync(_saveTask);
        await WaitQuietlyAsync(_loadTask);

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

            // The designer now counts the exported document as saved. A change event raised while it exported, an edit or
            // an undo, measured dirtiness against the state saved before, so it cannot tell whether the canvas still
            // differs from what was just exported. Such a canvas stays unsaved until the designer's next change event.
            _canvasDirty = _changeSequence != changeSequence;

            _unsavedExport = true;

            var result = await TryAsync(() => bpmnXmlService.PutXmlAsync(DefinitionId, xml, eTag, cancellationToken));

            if (!result.IsSuccess)
            {
                // The refusal names one problem; the analysis finds where it is.
                Analyze(xml);
                return Refuse(result.Failure!);
            }

            // The server imported what was sent, so nothing refuses it: its findings are the problems.
            if (_analyzer != null)
            {
                ShowProblems(BpmnDocumentProblem.From(result.Success!.Import.Analysis));
            }

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
    /// Has the server analyze <paramref name="xml"/> and shows what it reports as <see cref="Problems"/>, unless a later
    /// analysis or save result has replaced it by then. Without an analyzer, there are no problems to show.
    /// </summary>
    private void Analyze(string xml)
    {
        if (_analyzer == null)
        {
            return;
        }

        var sequence = ++_analysisSequence;
        _analysis = AnalyzeCoreAsync(_analyzer, xml, sequence);
    }

    private async Task AnalyzeCoreAsync(IBpmnInterchangeService analyzer, string xml, long sequence)
    {
        IReadOnlyList<BpmnDocumentProblem> problems;

        try
        {
            using var content = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var result = await analyzer.AnalyzeAsync(content, $"{DefinitionId}.bpmn", ProcessId);
            problems = result.IsSuccess ? BpmnDocumentProblem.From(result.Success!)
                : IsRefusalOfTheDocument(result.Failure!) ? BpmnDocumentProblem.From(result.Failure!)
                : [];
        }
        catch (Exception)
        {
            // The analysis only explains the document, so one that fails, such as for a server that cannot be reached,
            // leaves nothing to explain rather than taking the editor down.
            problems = [];
        }

        if (sequence == _analysisSequence)
        {
            await ShowProblemsAsync(problems);
        }
    }

    /// <summary>
    /// Whether a failed analysis says what is wrong with the document (<c>400</c> for one that cannot be read,
    /// <c>422</c> for one the reader refuses) rather than that the request failed, such as a <c>404</c> or a <c>500</c>,
    /// which says nothing about the document.
    /// </summary>
    private static bool IsRefusalOfTheDocument(ValidationErrors errors) =>
        errors.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity;

    /// <summary>Shows <paramref name="problems"/> in place of any analysis still in flight.</summary>
    private void ShowProblems(IReadOnlyList<BpmnDocumentProblem> problems)
    {
        _analysisSequence++;
        _analysis = ShowProblemsAsync(problems);
    }

    /// <summary>Makes <paramref name="problems"/> the session's problems and marks their elements on the canvas.</summary>
    private async Task ShowProblemsAsync(IReadOnlyList<BpmnDocumentProblem> problems)
    {
        Problems = problems;
        Changed?.Invoke();

        if (_canvas == null || CanvasLoadError != null)
        {
            return;
        }

        var markers = problems.SelectMany(problem => problem.ElementIds.Select(elementId => new BpmnDesignerMarker(elementId, problem.Severity, problem.Message))).ToList();

        try
        {
            await _canvas.ApplyMarkersAsync(markers);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Markers only point at the problems the panel lists; a designer that cannot show them leaves the list.
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

    private async Task LoadAfterAsync(Task? previousLoad, Task? save, CancellationToken cancellationToken)
    {
        await WaitQuietlyAsync(previousLoad);
        await WaitQuietlyAsync(save);
        await LoadAsync(cancellationToken);
    }

    /// <summary>Waits for <paramref name="task"/> without taking on its failure, which its own caller observes.</summary>
    private static async Task WaitQuietlyAsync(Task? task)
    {
        if (task == null)
            return;

        try
        {
            await task;
        }
        catch (Exception)
        {
            // The operation's own caller sees this failure; the operation waiting on it still has to run.
        }
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
