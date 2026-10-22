using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Tests.Support;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnModelerSession"/>: the designer's document is written back only through the XML PUT, carrying
/// the ETag of the revision it was edited from; a failed save leaves the document unsaved even though the designer
/// already exported it; and only a read, never a save, makes the canvas load the document again.
/// </summary>
public class BpmnModelerSessionTests
{
    private const string DefinitionId = "definition-1";
    private const string StoredXml = "<stored/>";
    private const string ETag = "\"REVISION-1\"";

    private readonly FakeBpmnXmlService _service = new();
    private readonly FakeModelerCanvas _canvas = new();
    private readonly BpmnModelerSession _session;

    public BpmnModelerSessionTests()
    {
        _service.ReturnsXml(StoredXml, ETag);
        _session = new(_service, DefinitionId);
    }

    [Fact(DisplayName = "A save sends the designer's document with the ETag it was loaded at, and the next save sends the new one")]
    public async Task Save_SendsTheDesignersDocumentAgainstTheLoadedRevision()
    {
        await OpenAsync();
        _service.AcceptsPut("\"REVISION-2\"");
        Edit();

        var first = await _session.SaveAsync();
        Edit();
        await _session.SaveAsync();

        Assert.True(first.IsSuccess);
        Assert.Equal([(DefinitionId, "<edited/>", ETag), (DefinitionId, "<edited/>", "\"REVISION-2\"")], _service.Puts);
        Assert.False(_session.IsDirty);
        Assert.Equal("<edited/>", _session.Xml);
        Assert.Equal(1, _service.GetCount);
    }

    [Fact(DisplayName = "A save does not make the canvas load the document again; a reload does")]
    public async Task OnlyAReadReplacesTheCanvasDocument()
    {
        await OpenAsync();
        var loadedVersion = _session.DocumentVersion;
        _service.AcceptsPut("\"REVISION-2\"");
        Edit();

        await _session.SaveAsync();
        Assert.Equal(loadedVersion, _session.DocumentVersion);

        await _session.ReloadAsync();
        Assert.Equal(loadedVersion + 1, _session.DocumentVersion);
    }

    [Fact(DisplayName = "A refused save stays unsaved, though the designer already counts its export as saved")]
    public async Task RefusedSave_KeepsTheDocumentDirty()
    {
        await OpenAsync();
        _service.RefusesPut(BpmnDocumentFailureReason.PreconditionFailed);
        Edit();

        var result = await _session.SaveAsync();
        // The designer would now report itself clean, for instance after an undo back to the exported state.
        _session.CanvasChanged(new([], IsDirty: false, CanUndo: true, CanRedo: false));

        Assert.Equal(BpmnDocumentFailureReason.PreconditionFailed, result.Failure!.Reason);
        Assert.Equal(BpmnDocumentFailureReason.PreconditionFailed, _session.SaveFailure!.Reason);
        Assert.True(_session.IsDirty);
        Assert.Single(_service.Puts);
    }

    [Fact(DisplayName = "An edit made while the designer exports keeps the document dirty after the save")]
    public async Task EditDuringExport_StaysDirty()
    {
        await OpenAsync();
        _service.AcceptsPut("\"REVISION-2\"");
        Edit();
        _canvas.DuringSave = Edit;

        await _session.SaveAsync();

        Assert.True(_session.IsDirty);
    }

    [Fact(DisplayName = "An undo made while the designer exports keeps the document dirty, though the designer called it clean")]
    public async Task UndoDuringExport_StaysDirty()
    {
        await OpenAsync();
        _service.AcceptsPut("\"REVISION-2\"");
        Edit();
        // Back to the state loaded before the save: clean against it, but no longer the document being saved.
        _canvas.DuringSave = () => _session.CanvasChanged(new(["Task_1"], IsDirty: false, CanUndo: false, CanRedo: true));

        await _session.SaveAsync();

        Assert.True(_session.IsDirty);
    }

    [Fact(DisplayName = "A reload asked for while a save is in flight reads the document only once the save has finished")]
    public async Task ReloadDuringSave_WaitsForTheSave()
    {
        await OpenAsync();
        _service.PutGate = new();
        _service.AcceptsPut("\"REVISION-2\"").ReturnsXml("<edited/>", "\"REVISION-2\"");
        Edit();

        var save = _session.SaveAsync();
        var reload = _session.ReloadAsync();

        Assert.Equal(1, _service.GetCount);

        _service.PutGate.SetResult();
        await save;
        await reload;

        Assert.Equal(2, _service.GetCount);
        Assert.Equal("<edited/>", _session.Xml);
    }

    [Fact(DisplayName = "A save asked for while the document is being read again is refused, since the read replaces what it would send")]
    public async Task SaveDuringReload_IsRefused()
    {
        await OpenAsync();
        _service.GetGate = new();
        Edit();

        var reload = _session.ReloadAsync();
        var result = await _session.SaveAsync();
        _service.GetGate.SetResult();
        await reload;

        Assert.False(result.IsSuccess);
        Assert.Empty(_service.Puts);
        Assert.Equal(0, _canvas.Saves);
    }

    [Fact(DisplayName = "Nothing is sent while the content the designer cannot keep is not acknowledged")]
    public async Task UnacknowledgedDroppedContent_SendsNothing()
    {
        await OpenAsync([new(BpmnDesignerLoadWarningKinds.Dropped, "Lane set 'Ls_1' is not shown.", "Ls_1")]);
        _service.AcceptsPut("\"REVISION-2\"");
        Edit();

        var refused = await _session.SaveAsync();
        _session.SaveGate.Acknowledge();
        var saved = await _session.SaveAsync();

        Assert.False(refused.IsSuccess);
        Assert.True(saved.IsSuccess);
        Assert.Single(_service.Puts);
        Assert.Equal(1, _canvas.Saves);
    }

    [Fact(DisplayName = "A designer that cannot export reports why, and nothing is sent")]
    public async Task ExportFailure_IsReported()
    {
        await OpenAsync();
        _canvas.SaveException = new InvalidOperationException("No document is loaded.");
        Edit();

        var result = await _session.SaveAsync();

        Assert.Contains("No document is loaded.", result.Failure!.Message);
        Assert.Empty(_service.Puts);
        Assert.True(_session.IsDirty);
    }

    [Fact(DisplayName = "A canvas that could not show the document has nothing to save")]
    public async Task CanvasLoadFailure_RefusesToSave()
    {
        await _session.EnsureLoadedAsync();
        _session.AttachCanvas(_canvas);
        _session.CanvasLoaded(new(false, "not BPMN", []));

        var result = await _session.SaveAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("not BPMN", _session.CanvasLoadError);
        Assert.Equal(0, _canvas.Saves);
    }

    [Fact(DisplayName = "Without an ETag in the response, the stored document is read back and its ETag adopted if it is the one sent")]
    public async Task MissingResponseETag_IsConfirmedByReadingBack()
    {
        await OpenAsync();
        _service.AcceptsPut(null).ReturnsXml("<edited/>", "\"REVISION-2\"");
        Edit();

        await _session.SaveAsync();
        Edit();
        await _session.SaveAsync();

        Assert.False(_session.SavedButRevisionUnknown);
        Assert.Equal("\"REVISION-2\"", _service.Puts[1].IfMatch);
    }

    [Fact(DisplayName = "Without an ETag in the response, a different stored document leaves the revision unknown, and the next save is refused as stale")]
    public async Task MissingResponseETag_WithAnotherWriter_LeavesTheRevisionUnknown()
    {
        await OpenAsync();
        _service.AcceptsPut(null).ReturnsXml("<someone-else/>", "\"REVISION-3\"");
        Edit();

        var result = await _session.SaveAsync();

        Assert.True(result.IsSuccess);
        Assert.True(_session.SavedButRevisionUnknown);

        Edit();
        var next = await _session.SaveAsync();

        Assert.Equal(BpmnDocumentFailureReason.PreconditionFailed, next.Failure!.Reason);
        Assert.Single(_service.Puts);
    }

    [Fact(DisplayName = "After an ordinary save, an unchanged document's new ETag is adopted; a changed one conflicts with unsaved edits")]
    public async Task RefreshAfterOrdinarySave_AdoptsOrRefuses()
    {
        await OpenAsync();
        _service.ReturnsXml(StoredXml, "\"REVISION-2\"");

        Assert.True(await _session.RefreshRevisionAfterOrdinarySaveAsync());

        Edit();
        _service.ReturnsXml("<changed/>", "\"REVISION-3\"");

        Assert.False(await _session.RefreshRevisionAfterOrdinarySaveAsync());
        Assert.Equal(BpmnDocumentFailureReason.PreconditionFailed, _session.SaveFailure!.Reason);
        Assert.Equal(StoredXml, _session.Xml);

        _service.AcceptsPut("\"REVISION-4\"");
        await _session.SaveAsync();
        Assert.Equal("\"REVISION-2\"", Assert.Single(_service.Puts).IfMatch);
    }

    [Fact(DisplayName = "After an ordinary save, a changed document replaces one the designer has nothing unsaved in")]
    public async Task RefreshAfterOrdinarySave_WithNothingUnsaved_TakesTheChangedDocument()
    {
        await OpenAsync();
        var version = _session.DocumentVersion;
        _service.ReturnsXml("<changed/>", "\"REVISION-3\"");

        Assert.True(await _session.RefreshRevisionAfterOrdinarySaveAsync());

        Assert.Equal("<changed/>", _session.Xml);
        Assert.Equal(version + 1, _session.DocumentVersion);
        Assert.Null(_session.SaveFailure);
    }

    [Fact(DisplayName = "A read failure is reported, and nothing can be saved")]
    public async Task ReadFailure_IsReported()
    {
        var session = new BpmnModelerSession(new FakeBpmnXmlService().RefusesGet(BpmnDocumentFailureReason.SourceStale), DefinitionId);

        await session.EnsureLoadedAsync();
        session.AttachCanvas(_canvas);
        var result = await session.SaveAsync();

        Assert.Equal(BpmnDocumentFailureReason.SourceStale, session.LoadFailure!.Reason);
        Assert.Null(session.Xml);
        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "Undo and redo go to the attached designer, and its change event says what is possible")]
    public async Task UndoRedo_GoToTheDesigner()
    {
        await OpenAsync();

        _session.CanvasChanged(new(["Task_1"], IsDirty: true, CanUndo: true, CanRedo: true));
        await _session.UndoAsync();
        await _session.RedoAsync();

        Assert.True(_session.CanUndo);
        Assert.True(_session.CanRedo);
        Assert.Equal((1, 1), (_canvas.Undos, _canvas.Redos));
    }

    private async Task OpenAsync(IReadOnlyList<BpmnDesignerLoadWarning>? warnings = null)
    {
        await _session.EnsureLoadedAsync();
        _session.AttachCanvas(_canvas);
        _session.CanvasLoaded(new(true, null, warnings ?? []));
    }

    private void Edit() => _session.CanvasChanged(new(["Task_1"], IsDirty: true, CanUndo: true, CanRedo: false));
}
