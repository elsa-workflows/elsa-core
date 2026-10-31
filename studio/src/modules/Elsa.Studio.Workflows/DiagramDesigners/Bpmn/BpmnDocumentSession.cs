using System.Text.Json.Nodes;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// One BPMN-imported workflow definition's <c>bpmnDefinitions</c> document, between the document <c>GET</c> that read
/// it and the document <c>PUT</c> that writes an edit back: the revision the server last returned, and a working copy
/// the "Performed by" section edits in place.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> A BPMN-imported definition's activity graph is derived from its document. An edit to a
/// bound activity has to be written into the document and saved through the document <c>PUT</c>, which re-imports
/// it; the ordinary workflow-definition save would rewrite the graph behind the document, which elsa-core then refuses
/// to read or export as stale. This class is the only thing that writes a binding, and it only ever writes one
/// through <see cref="SaveAsync"/>.
/// </para>
/// <para>
/// <b>Optimistic concurrency.</b> Every <c>PUT</c> carries, as <c>If-Match</c>, the <c>ETag</c> of the revision the
/// edit was made against. A refusal (<see cref="BpmnDocumentFailureReason.PreconditionFailed"/>) is reported, never
/// retried: the working copy stays as it is until the user reloads, which discards it.
/// </para>
/// </remarks>
public sealed class BpmnDocumentSession(IBpmnInterchangeService bpmnInterchangeService, string definitionId)
{
    private readonly SortedSet<string> _editedElementIds = new(StringComparer.Ordinal);
    private BpmnDocumentRevision? _revision;
    private Task? _loadTask;
    private Task<Result<BpmnDocumentSaveResult, BpmnDocumentFailure>>? _saveTask;
    private bool _isSaving;

    /// <summary>The workflow definition whose document this is.</summary>
    public string DefinitionId { get; } = definitionId;

    /// <summary>
    /// Raised whenever <see cref="IsSaving"/> flips, so a component holding this session across an <see langword="await"/>
    /// — one that would otherwise not re-render until the whole operation that changed it has finished — can answer with
    /// its own <c>InvokeAsync(StateHasChanged)</c> and show the saving state while it is current.
    /// </summary>
    public event Action? Changed;

    /// <summary>The working copy, with every unsaved edit applied, or <see langword="null"/> until a read succeeds.</summary>
    public JsonObject? Document { get; private set; }

    /// <summary>Whether a read is in flight.</summary>
    public bool IsLoading => _loadTask is { IsCompleted: false };

    /// <summary>
    /// Whether the working copy is being written back through the document <c>PUT</c>, or the read <see cref="SaveAsync"/>
    /// follows it with. Set for that whole window, so an edit made after the body was serialized cannot be silently lost
    /// to the reload that follows a successful <c>PUT</c>: <see cref="SetBinding"/> refuses while this is
    /// <see langword="true"/>.
    /// </summary>
    public bool IsSaving => _isSaving;

    /// <summary>Why the last read failed, or <see langword="null"/> when it did not.</summary>
    public BpmnDocumentFailure? LoadFailure { get; private set; }

    /// <summary>Why the last save failed, or <see langword="null"/> when it did not or an edit has been made since.</summary>
    public BpmnDocumentFailure? SaveFailure { get; private set; }

    /// <summary>
    /// Whether the last <see cref="SaveAsync"/> wrote the edit successfully — it is on the server — but the read that
    /// should have refreshed the working copy with the server's own view of it then failed, leaving <see cref="Document"/>
    /// cleared and <see cref="LoadFailure"/> set. Nothing was lost; only the local view of it could not be confirmed.
    /// </summary>
    public bool SavedButReloadFailed { get; private set; }

    /// <summary>The elements whose binding the working copy changed since the document was last read or discarded.</summary>
    public IReadOnlyCollection<string> EditedElementIds => _editedElementIds;

    /// <summary>Whether the working copy differs from the revision the server last returned.</summary>
    public bool IsDirty => Document != null && _revision != null && !JsonNode.DeepEquals(Document, _revision.Document);

    /// <summary>Reads the document, unless a read has already started.</summary>
    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => _loadTask ??= LoadAsync(cancellationToken);

    /// <summary>Reads the document again, discarding the working copy and any unsaved edit in it.</summary>
    public Task ReloadAsync(CancellationToken cancellationToken = default) => _loadTask = LoadAsync(cancellationToken);

    /// <summary>The element of the working copy with <paramref name="elementId"/>, or <see langword="null"/>.</summary>
    public JsonObject? FindElement(string elementId) => Document == null ? null : BpmnDefinitionsDocument.FindElement(Document, elementId);

    /// <summary>
    /// Makes <paramref name="binding"/> the activity binding of the element with <paramref name="elementId"/> in the
    /// working copy. Nothing else in the document changes, and nothing is sent until <see cref="SaveAsync"/>.
    /// </summary>
    /// <returns>
    /// <see langword="false"/>, leaving the working copy unchanged, while a save is in flight (<see cref="IsSaving"/>):
    /// the document just sent is being read back, and an edit applied to it now would be overwritten by that read
    /// without ever being saved. The UI is expected to disable editing for that window; this is only the backstop for
    /// an edit that reaches the session anyway. Otherwise <see langword="true"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">The working copy has no element with <paramref name="elementId"/>.</exception>
    public bool SetBinding(string elementId, JsonObject binding)
    {
        if (_isSaving)
            return false;

        var element = FindElement(elementId) ?? throw new InvalidOperationException($"The BPMN document has no element '{elementId}'.");

        BpmnActivityBindingFormat.Attach(element, binding);
        _editedElementIds.Add(elementId);
        SaveFailure = null;
        return true;
    }

    /// <summary>Throws away every unsaved edit, restoring the revision the server last returned.</summary>
    public void Discard()
    {
        if (_revision != null)
            Document = (JsonObject)_revision.Document.DeepClone();

        _editedElementIds.Clear();
        SaveFailure = null;
        SavedButReloadFailed = false;
    }

    /// <summary>
    /// Writes the working copy back through the document <c>PUT</c>, against the revision it was edited from, then reads
    /// the document back so the session holds the server's own view and the new <c>ETag</c>. <see cref="IsSaving"/> is
    /// set for the whole of that window, so an edit attempted while it runs is refused rather than silently lost to the
    /// reload; see <see cref="SetBinding"/>.
    /// </summary>
    /// <remarks>
    /// Single-flight: a call made while a save is already in flight does not send a second <c>PUT</c>. It returns the
    /// same task as the call already running, so both callers observe the one save's outcome. This is exact, not a
    /// lossy best-effort, because <see cref="SetBinding"/> refuses edits for as long as <see cref="IsSaving"/> is
    /// <see langword="true"/>: the working copy cannot have changed since the in-flight <c>PUT</c> was sent, so there is
    /// nothing a second <c>PUT</c> could carry that the first one does not already.
    /// </remarks>
    /// <returns>The server's result on success, or why the document was not saved.</returns>
    public Task<Result<BpmnDocumentSaveResult, BpmnDocumentFailure>> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (_saveTask is { IsCompleted: false })
            return _saveTask;

        if (Document == null || _revision == null)
            return Task.FromResult(Refuse(new(BpmnDocumentFailureReason.Unknown, "The BPMN document has not been read, so there is nothing to save.")));

        return _saveTask = SaveCoreAsync(Document, _revision, cancellationToken);
    }

    private async Task<Result<BpmnDocumentSaveResult, BpmnDocumentFailure>> SaveCoreAsync(JsonObject document, BpmnDocumentRevision revision, CancellationToken cancellationToken)
    {
        SavedButReloadFailed = false;
        _isSaving = true;
        Changed?.Invoke();

        try
        {
            var result = await TryAsync(() => bpmnInterchangeService.PutDocumentAsync(DefinitionId, document, revision.ETag, cancellationToken));

            if (!result.IsSuccess)
                return Refuse(result.Failure!);

            await ReloadAsync(cancellationToken);

            // The PUT is on the server; only the read that was to confirm it locally failed. That is not the failure
            // SaveFailure reports — a retry would resend an edit the server already has — so it is reported separately.
            SavedButReloadFailed = LoadFailure != null;
            return result;
        }
        finally
        {
            _isSaving = false;
            _saveTask = null;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Called after an ordinary workflow-definition save that may have advanced this document's revision without
    /// changing what it describes — re-serializing the graph, or a publish that opens a new draft, can move the
    /// <c>ETag</c> without changing the content a BPMN import would produce from it. Re-reads the document and, if its
    /// content still equals the revision the working copy was built from, adopts the new <c>ETag</c> so
    /// <see cref="SaveAsync"/> does not carry a stale one and get refused for a change nobody made. If the content
    /// differs, that is a genuine conflict, reported the same way a stale <c>PUT</c> would be, without sending anything.
    /// If the re-read itself fails, that failure is reported instead.
    /// </summary>
    /// <returns><see langword="true"/> if <see cref="SaveAsync"/> may proceed; otherwise the refusal is in <see cref="SaveFailure"/>.</returns>
    public async Task<bool> RefreshRevisionAfterOrdinarySaveAsync(CancellationToken cancellationToken = default)
    {
        // A save already in flight will itself re-read the document and adopt the revision it finds; racing a second
        // read against that PUT could compare against a revision the PUT is about to make stale, and refuse this save
        // for a change nobody but the in-flight save itself made. Waiting for it first compares against what it left.
        if (_saveTask is { IsCompleted: false })
            await _saveTask;

        if (Document == null || _revision == null)
            return false;

        var result = await TryAsync(() => bpmnInterchangeService.GetDocumentAsync(DefinitionId, cancellationToken));

        if (!result.IsSuccess)
        {
            Refuse(result.Failure!);
            return false;
        }

        var fetched = result.Success!;

        if (!JsonNode.DeepEquals(fetched.Document, _revision.Document))
        {
            Refuse(new(
                BpmnDocumentFailureReason.PreconditionFailed,
                "This workflow was changed since its BPMN document was read, by another save or another user, so the binding changes were not saved."));
            return false;
        }

        _revision = fetched;
        SaveFailure = null;
        return true;
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await TryAsync(() => bpmnInterchangeService.GetDocumentAsync(DefinitionId, cancellationToken));

        _revision = result.Success;
        Document = (JsonObject?)_revision?.Document.DeepClone();
        LoadFailure = result.Failure;
        SaveFailure = null;
        SavedButReloadFailed = false;
        _editedElementIds.Clear();
    }

    /// <summary>
    /// Reports a request that never got an answer — the server unreachable, the connection dropped — as a failure the
    /// section shows, rather than an exception that would take the whole editor down with it.
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

    private Result<BpmnDocumentSaveResult, BpmnDocumentFailure> Refuse(BpmnDocumentFailure failure)
    {
        SaveFailure = failure;
        return new(failure);
    }
}
