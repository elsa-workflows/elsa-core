using System.Net;
using System.Text.Json;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Tests.Support;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnModelerSession.Problems"/>: the server analyzes each document the canvas loads and each one a
/// refused save sends, a successful save reports its own findings, and the elements every problem names are marked on
/// the canvas.
/// </summary>
public class BpmnModelerSessionProblemsTests
{
    private const string DefinitionId = "definition-1";
    private const string StoredXml = "<stored/>";
    private const string ETag = "\"REVISION-1\"";

    private readonly FakeBpmnXmlService _xml = new();
    private readonly FakeBpmnDocumentService _analyzer = new();
    private readonly FakeModelerCanvas _canvas = new();
    private readonly BpmnModelerSession _session;

    public BpmnModelerSessionProblemsTests()
    {
        _xml.ReturnsXml(StoredXml, ETag);
        _session = new(_xml, _analyzer, DefinitionId);
    }

    [Fact(DisplayName = "The document the canvas loads is analyzed: its refusal and the findings that lose meaning are problems, and their elements are marked")]
    public async Task ALoadedDocument_IsAnalyzed()
    {
        _analyzer.Analyzes = _ => Analysis(
            new BpmnImportRefusalModel { Code = BpmnErrorCodes.ImportBindingInvalid, Message = "Charge is unbound.", ElementIds = ["Charge"] },
            Issue("Info", "Kept.", "Start"),
            Issue("Degraded", "Timer simplified.", "Wait"),
            Issue("Dropped", "Lane dropped.", null));

        await OpenAsync();

        Assert.Equal([StoredXml], _analyzer.Analyzed);
        Assert.Equal(
            [
                (BpmnDesignerMarker.Error, "Charge is unbound.", "Charge"),
                (BpmnDesignerMarker.Warning, "Timer simplified.", "Wait"),
                (BpmnDesignerMarker.Warning, "Lane dropped.", "")
            ],
            _session.Problems.Select(problem => (problem.Severity, problem.Message, string.Join(",", problem.ElementIds))));
        Assert.Equal(
            [new("Charge", BpmnDesignerMarker.Error, "Charge is unbound."), new("Wait", BpmnDesignerMarker.Warning, "Timer simplified.")],
            Assert.Single(_canvas.AppliedMarkers));
    }

    [Fact(DisplayName = "A refused save has the document it sent analyzed, so the problems point at what refused it")]
    public async Task ARefusedSave_AnalyzesTheSentDocument()
    {
        _analyzer.Analyzes = _ => Analysis();
        await OpenAsync();
        _xml.RefusesPut(BpmnDocumentFailureReason.BindingInvalid);
        _analyzer.Analyzes = _ => Analysis(new BpmnImportRefusalModel { Code = BpmnErrorCodes.ImportBindingInvalid, Message = "Charge is unbound.", ElementIds = ["Charge"] });
        Edit();

        await _session.SaveAsync();
        await _session.Analysis;

        Assert.Equal([StoredXml, _canvas.Xml], _analyzer.Analyzed);
        Assert.Equal(["Charge"], Assert.Single(_session.Problems).ElementIds);
        Assert.Equal([new("Charge", BpmnDesignerMarker.Error, "Charge is unbound.")], _canvas.AppliedMarkers[^1]);
    }

    [Fact(DisplayName = "A successful save reports the findings of its own import, without another analysis")]
    public async Task ASuccessfulSave_ReportsItsOwnFindings()
    {
        _analyzer.Analyzes = _ => Analysis(new BpmnImportRefusalModel { Code = BpmnErrorCodes.ImportBindingInvalid, Message = "Charge is unbound.", ElementIds = ["Charge"] });
        await OpenAsync();
        _xml.AcceptsPutReporting("\"REVISION-2\"", new BpmnImportAnalysisModel { Issues = [Issue("Degraded", "Timer simplified.", "Wait")] });
        Edit();

        await _session.SaveAsync();
        await _session.Analysis;

        Assert.Single(_analyzer.Analyzed);
        Assert.Equal(["Wait"], Assert.Single(_session.Problems).ElementIds);
        Assert.Equal([new("Wait", BpmnDesignerMarker.Warning, "Timer simplified.")], _canvas.AppliedMarkers[^1]);
    }

    [Fact(DisplayName = "An analysis that finishes after a later save's findings does not replace them")]
    public async Task AStaleAnalysis_IsIgnored()
    {
        var analysis = new TaskCompletionSource<Result<BpmnImportAnalysisModel, ValidationErrors>>();
        _analyzer.Analyzes = _ => analysis.Task;
        await _session.EnsureLoadedAsync();
        _session.AttachCanvas(_canvas);
        _session.CanvasLoaded(new(true, null, []), _session.DocumentVersion);
        _xml.AcceptsPut("\"REVISION-2\"");
        Edit();

        await _session.SaveAsync();
        analysis.SetResult(new(new BpmnImportAnalysisModel { Refusal = new() { Message = "Stale.", ElementIds = ["Charge"] } }));
        await Task.Yield();

        Assert.Empty(_session.Problems);
        Assert.Equal([[]], _canvas.AppliedMarkers);
    }

    [Fact(DisplayName = "An analysis the server refuses lists its errors, with the elements its data names")]
    public async Task ARefusedAnalysis_ListsItsErrors()
    {
        var data = JsonSerializer.SerializeToElement(new { elementIds = new[] { "Task_1" } });
        _analyzer.Analyzes = _ => Task.FromResult(new Result<BpmnImportAnalysisModel, ValidationErrors>(
            new ValidationErrors([new("Two elements share the id 'Task_1'.")], HttpStatusCode.UnprocessableEntity, "bpmn.import.duplicate-element-id", data)));

        await OpenAsync();

        var problem = Assert.Single(_session.Problems);
        Assert.Equal((BpmnDesignerMarker.Error, "Two elements share the id 'Task_1'."), (problem.Severity, problem.Message));
        Assert.Equal(["Task_1"], problem.ElementIds);
    }

    [Fact(DisplayName = "An analysis that fails, such as for a server that cannot be reached, leaves no problems")]
    public async Task AFailedAnalysis_LeavesNoProblems()
    {
        _analyzer.Analyzes = _ => throw new HttpRequestException("unreachable");

        await OpenAsync();

        Assert.Empty(_session.Problems);
        Assert.Equal([[]], _canvas.AppliedMarkers);
    }

    [Fact(DisplayName = "A session created without an analyzer reports no problems, not even a save's own findings, and marks nothing")]
    public async Task WithoutAnAnalyzer_ThereAreNoProblems()
    {
        var session = new BpmnModelerSession(_xml, DefinitionId);
        await session.EnsureLoadedAsync();
        session.AttachCanvas(_canvas);
        session.CanvasLoaded(new(true, null, []), session.DocumentVersion);
        _xml.AcceptsPutReporting("\"REVISION-2\"", new BpmnImportAnalysisModel { Issues = [Issue("Degraded", "Timer simplified.", "Wait")] });
        session.CanvasChanged(new(["Charge"], IsDirty: true, CanUndo: true, CanRedo: false));
        await session.SaveAsync();
        await session.Analysis;

        Assert.Empty(session.Problems);
        Assert.Empty(_canvas.AppliedMarkers);
    }

    [Fact(DisplayName = "Selecting a problem's element selects it on the canvas")]
    public async Task SelectAsync_SelectsOnTheCanvas()
    {
        _analyzer.Analyzes = _ => Analysis();
        await OpenAsync();

        Assert.True(await _session.SelectAsync("Charge"));
        Assert.Equal(["Charge"], _canvas.Selections);
    }

    private static Task<Result<BpmnImportAnalysisModel, ValidationErrors>> Analysis(BpmnImportRefusalModel? refusal = null, params BpmnImportIssueModel[] issues) =>
        Task.FromResult(new Result<BpmnImportAnalysisModel, ValidationErrors>(new BpmnImportAnalysisModel { Refusal = refusal, Issues = issues }));

    private static BpmnImportIssueModel Issue(string severity, string message, string? elementId) => new() { Severity = severity, Message = message, ElementId = elementId };

    private async Task OpenAsync()
    {
        await _session.EnsureLoadedAsync();
        _session.AttachCanvas(_canvas);
        _session.CanvasLoaded(new(true, null, []), _session.DocumentVersion);
        await _session.Analysis;
    }

    private void Edit() => _session.CanvasChanged(new(["Charge"], IsDirty: true, CanUndo: true, CanRedo: false));
}
