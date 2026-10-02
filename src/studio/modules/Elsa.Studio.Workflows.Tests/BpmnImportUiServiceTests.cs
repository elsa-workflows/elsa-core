using System.Net;
using System.Text.Json;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnImportUiService"/>'s per-file import flow. <see cref="ImportAsync_ReturnsAFailedResult_WhenTheImportedDefinitionCannotBeFound"/>
/// pins that a successful import whose definition cannot be re-fetched is reported as a failure rather than as a
/// success carrying a null <see cref="WorkflowImportResult.WorkflowDefinition"/>, which is what its two callers
/// dereference unconditionally.
/// </summary>
public sealed class BpmnImportUiServiceTests : BunitContext, IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public BpmnImportUiServiceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task ImportAsync_ReturnsAFailedResult_WhenTheImportedDefinitionCannotBeFound()
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var localizer = Services.GetRequiredService<ILocalizer>();
        var interchangeService = new FakeBpmnInterchangeService
        {
            AnalyzeResult = new(new BpmnImportAnalysisModel { ProcessIds = ["only-process"] }),
            ImportResult = new(new BpmnImportResultModel { DefinitionId = "wf-1", Version = 1 })
        };
        var definitionService = new FakeWorkflowDefinitionService { DefinitionToReturn = null };
        var service = new BpmnImportUiService(dialogService, localizer, interchangeService, definitionService);
        var file = new FakeBrowserFile("process.bpmn");

        var importTask = _dialogProvider.InvokeAsync(() => service.ImportFileAsync(file, definitionId: null));
        _dialogProvider.WaitForElement("button");
        await _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

        var result = await importTask.WaitAsync(Timeout);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Null(result.WorkflowDefinition);
        Assert.Equal(WorkflowImportFailureType.Exception, result.Failure!.FailureType);
        Assert.Equal("wf-1", definitionService.RequestedDefinitionId);
    }

    [Fact]
    public async Task ImportBpmnFilesAsync_SplitsBpmnFilesFromOtherFiles_AndImportsEachBpmnFileInOrder()
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var localizer = Services.GetRequiredService<ILocalizer>();
        var interchangeService = new FakeBpmnInterchangeService
        {
            AnalyzeResult = new(new BpmnImportAnalysisModel { ProcessIds = ["only-process"] }),
            ImportResult = new(new BpmnImportResultModel { DefinitionId = "wf-1", Version = 1 })
        };
        var definitionService = new FakeWorkflowDefinitionService { DefinitionToReturn = new WorkflowDefinition { DefinitionId = "wf-1" } };
        var service = new BpmnImportUiService(dialogService, localizer, interchangeService, definitionService);
        var files = new IBrowserFile[]
        {
            new FakeBrowserFile("a.bpmn"),
            new FakeBrowserFile("data.json"),
            new FakeBrowserFile("b.bpmn")
        };

        var batchTask = _dialogProvider.InvokeAsync(() => service.ImportBpmnFilesAsync(files, definitionId: null));

        // "a.bpmn"'s findings dialog.
        _dialogProvider.WaitForElement("button");
        await _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

        // "b.bpmn"'s findings dialog.
        _dialogProvider.WaitForElement("button");
        await _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

        var batch = await batchTask.WaitAsync(Timeout);

        Assert.Equal(2, batch.Results.Count);
        Assert.Equal("a.bpmn", batch.Results[0].FileName);
        Assert.Equal("b.bpmn", batch.Results[1].FileName);
        Assert.True(batch.Results[0].IsSuccess);
        Assert.True(batch.Results[1].IsSuccess);
        var otherFile = Assert.Single(batch.OtherFiles);
        Assert.Equal("data.json", otherFile.Name);
    }

    [Fact]
    public async Task ImportBpmnFilesAsync_FailsEveryFile_WhenMoreThanOneBpmnFileTargetsAnOpenDefinition()
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var localizer = Services.GetRequiredService<ILocalizer>();
        var interchangeService = new FakeBpmnInterchangeService();
        var definitionService = new FakeWorkflowDefinitionService();
        var service = new BpmnImportUiService(dialogService, localizer, interchangeService, definitionService);
        var files = new IBrowserFile[]
        {
            new FakeBrowserFile("a.bpmn"),
            new FakeBrowserFile("b.bpmn")
        };

        var batch = await service.ImportBpmnFilesAsync(files, definitionId: "wf-1");

        Assert.Equal(2, batch.Results.Count);
        Assert.All(batch.Results, result => Assert.False(result.IsSuccess));
        Assert.Equal("a.bpmn", batch.Results[0].FileName);
        Assert.Equal("b.bpmn", batch.Results[1].FileName);
        Assert.Equal(0, interchangeService.AnalyzeCallCount);
        Assert.Equal(0, interchangeService.ImportCallCount);
        Assert.Null(definitionService.RequestedDefinitionId);
    }

    [Fact]
    public async Task ImportBpmnFilesAsync_ImportsTheSingleBpmnFile_IntoTheOpenDefinition()
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var localizer = Services.GetRequiredService<ILocalizer>();
        var interchangeService = new FakeBpmnInterchangeService
        {
            AnalyzeResult = new(new BpmnImportAnalysisModel { ProcessIds = ["only-process"] }),
            ImportResult = new(new BpmnImportResultModel { DefinitionId = "wf-1", Version = 1 })
        };
        var definitionService = new FakeWorkflowDefinitionService { DefinitionToReturn = new WorkflowDefinition { DefinitionId = "wf-1" } };
        var service = new BpmnImportUiService(dialogService, localizer, interchangeService, definitionService);
        var files = new IBrowserFile[] { new FakeBrowserFile("a.bpmn") };

        var batchTask = _dialogProvider.InvokeAsync(() => service.ImportBpmnFilesAsync(files, definitionId: "wf-1"));

        _dialogProvider.WaitForElement("button");
        await _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

        var batch = await batchTask.WaitAsync(Timeout);

        var result = Assert.Single(batch.Results);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, interchangeService.ImportCallCount);
        Assert.Equal("wf-1", interchangeService.LastImportDefinitionId);
    }

    [Fact]
    public async Task ImportFileAsync_ShowsTheRefusalDialogOnce_AndReturnsACapabilityRefusalResult_ForTheCodedRefusal()
    {
        const string message =
            "This deployment does not declare the following BPMN host capabilities the document requires: ScopeSignalling. "
            + "Offending elements (combined across all missing capabilities above, not attributable to any one of them): Gateway_1.";

        var dialogService = Services.GetRequiredService<IDialogService>();
        var localizer = Services.GetRequiredService<ILocalizer>();
        var interchangeService = new FakeBpmnInterchangeService
        {
            AnalyzeResult = new(new BpmnImportAnalysisModel { ProcessIds = ["only-process"] }),
            ImportResult = new(new ValidationErrors(
                [new ValidationError(message)],
                HttpStatusCode.UnprocessableEntity,
                Code: BpmnErrorCodes.ImportCapabilityUnsupported,
                Data: JsonDocument.Parse("""{ "capabilities": ["ScopeSignalling"], "elementIds": ["Gateway_1"] }""").RootElement))
        };
        var definitionService = new FakeWorkflowDefinitionService();
        var service = new BpmnImportUiService(dialogService, localizer, interchangeService, definitionService);
        var file = new FakeBrowserFile("process.bpmn");

        var importTask = _dialogProvider.InvokeAsync(() => service.ImportFileAsync(file, definitionId: null));

        // The findings dialog.
        _dialogProvider.WaitForElement("button");
        await _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

        // The refusal dialog, shown exactly once; closing it lets the import finish.
        _dialogProvider.WaitForElement("button");
        Assert.Contains("ScopeSignalling", _dialogProvider.Markup);
        Assert.Contains("Gateway_1", _dialogProvider.Markup);
        await _dialogProvider.InvokeAsync(() => CloseButton().ClickAsync(new MouseEventArgs()));

        var result = await importTask.WaitAsync(Timeout);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(WorkflowImportFailureType.CapabilityRefusal, result.Failure!.FailureType);
        Assert.Null(definitionService.RequestedDefinitionId);

        // The caller-side summary path excludes a result already reported in its own dialog.
        Assert.Empty(BpmnImportBatch.ReportableResults([result]));
    }

    [Fact]
    public async Task ImportFileAsync_FallsBackToTheServerMessage_ForA422WithAnUnrecognizedCode()
    {
        const string message = "Some other 422 refusal that is not a capability refusal.";

        var dialogService = Services.GetRequiredService<IDialogService>();
        var localizer = Services.GetRequiredService<ILocalizer>();
        var interchangeService = new FakeBpmnInterchangeService
        {
            AnalyzeResult = new(new BpmnImportAnalysisModel { ProcessIds = ["only-process"] }),
            ImportResult = new(new ValidationErrors([new ValidationError(message)], HttpStatusCode.UnprocessableEntity, Code: BpmnErrorCodes.ImportBindingInvalid))
        };
        var definitionService = new FakeWorkflowDefinitionService();
        var service = new BpmnImportUiService(dialogService, localizer, interchangeService, definitionService);
        var file = new FakeBrowserFile("process.bpmn");

        var importTask = _dialogProvider.InvokeAsync(() => service.ImportFileAsync(file, definitionId: null));

        // The findings dialog; there is no refusal dialog to close afterwards because this code is not the
        // capability refusal's.
        _dialogProvider.WaitForElement("button");
        await _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

        var result = await importTask.WaitAsync(Timeout);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(WorkflowImportFailureType.Exception, result.Failure!.FailureType);
        Assert.Equal(message, result.Failure.ErrorMessage);
    }

    [Fact]
    public async Task ImportFileAsync_FallsBackToTheServerMessage_ForACapabilityRefusalCodeWithNoData()
    {
        const string message = "This deployment does not declare a required BPMN host capability.";

        var dialogService = Services.GetRequiredService<IDialogService>();
        var localizer = Services.GetRequiredService<ILocalizer>();
        var interchangeService = new FakeBpmnInterchangeService
        {
            AnalyzeResult = new(new BpmnImportAnalysisModel { ProcessIds = ["only-process"] }),
            ImportResult = new(new ValidationErrors([new ValidationError(message)], HttpStatusCode.UnprocessableEntity, Code: BpmnErrorCodes.ImportCapabilityUnsupported))
        };
        var definitionService = new FakeWorkflowDefinitionService();
        var service = new BpmnImportUiService(dialogService, localizer, interchangeService, definitionService);
        var file = new FakeBrowserFile("process.bpmn");

        var importTask = _dialogProvider.InvokeAsync(() => service.ImportFileAsync(file, definitionId: null));

        // The findings dialog; there is no refusal dialog to close afterwards because a coded refusal with no
        // `data` cannot be read into a `BpmnCapabilityRefusal`, so it falls back to the ordinary failure path.
        _dialogProvider.WaitForElement("button");
        await _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

        var result = await importTask.WaitAsync(Timeout);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(WorkflowImportFailureType.Exception, result.Failure!.FailureType);
        Assert.Equal(message, result.Failure.ErrorMessage);
    }

    [Fact]
    public async Task ImportFileAsync_DoesNotTreatAnUncodedMessage_AsACapabilityRefusal_EvenWithTheRefusalWording()
    {
        // An older server that has not been upgraded to send `code` yet: without it, the message is shown as an
        // ordinary failure rather than the dialog, even though its wording matches the capability refusal's.
        const string message =
            "This deployment does not declare the following BPMN host capabilities the document requires: ScopeSignalling. "
            + "Offending elements (combined across all missing capabilities above, not attributable to any one of them): Gateway_1.";

        var dialogService = Services.GetRequiredService<IDialogService>();
        var localizer = Services.GetRequiredService<ILocalizer>();
        var interchangeService = new FakeBpmnInterchangeService
        {
            AnalyzeResult = new(new BpmnImportAnalysisModel { ProcessIds = ["only-process"] }),
            ImportResult = new(new ValidationErrors([new ValidationError(message)], HttpStatusCode.UnprocessableEntity))
        };
        var definitionService = new FakeWorkflowDefinitionService();
        var service = new BpmnImportUiService(dialogService, localizer, interchangeService, definitionService);
        var file = new FakeBrowserFile("process.bpmn");

        var importTask = _dialogProvider.InvokeAsync(() => service.ImportFileAsync(file, definitionId: null));

        // The findings dialog; there is no refusal dialog to close afterwards because a body with no code never
        // counts as one.
        _dialogProvider.WaitForElement("button");
        await _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

        var result = await importTask.WaitAsync(Timeout);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Equal(WorkflowImportFailureType.Exception, result.Failure!.FailureType);
    }

    private AngleSharp.Dom.IElement ImportButton() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Import");
    private AngleSharp.Dom.IElement CloseButton() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Close");

    private sealed class FakeBrowserFile(string name) : IBrowserFile
    {
        public string Name { get; } = name;
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => 0;
        public string ContentType => "application/xml";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream();
    }

    private sealed class FakeBpmnInterchangeService : IBpmnInterchangeService
    {
        public Result<BpmnImportAnalysisModel, ValidationErrors> AnalyzeResult { get; set; } = new(new BpmnImportAnalysisModel());
        public Result<BpmnImportResultModel, ValidationErrors> ImportResult { get; set; } = new(new BpmnImportResultModel());
        public int AnalyzeCallCount { get; private set; }
        public int ImportCallCount { get; private set; }
        public string? LastImportDefinitionId { get; private set; }

        public Task<Result<BpmnImportAnalysisModel, ValidationErrors>> AnalyzeAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            AnalyzeCallCount++;
            return Task.FromResult(AnalyzeResult);
        }

        public Task<Result<BpmnImportResultModel, ValidationErrors>> ImportAsync(Stream content, string fileName, string? definitionId, string? name, string? processId, CancellationToken cancellationToken = default)
        {
            ImportCallCount++;
            LastImportDefinitionId = definitionId;
            return Task.FromResult(ImportResult);
        }

        public Task<Result<FileDownload, BpmnExportFailure>> ExportAsync(string definitionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<BpmnDocumentRevision, BpmnDocumentFailure>> GetDocumentAsync(string definitionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<BpmnDocumentSaveResult, BpmnDocumentFailure>> PutDocumentAsync(string definitionId, System.Text.Json.Nodes.JsonObject document, string eTag, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// A stub that only implements <see cref="FindByDefinitionIdAsync"/>; every other member throws to flag an
    /// unexpected call.
    /// </summary>
    private sealed class FakeWorkflowDefinitionService : IWorkflowDefinitionService
    {
        public WorkflowDefinition? DefinitionToReturn { get; set; }
        public string? RequestedDefinitionId { get; private set; }

        public Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default)
        {
            RequestedDefinitionId = definitionId;
            return Task.FromResult(DefinitionToReturn);
        }

        public Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<WorkflowDefinition>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ActivityNode?> FindSubgraphAsync(string id, string? parentNodeId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GetPathSegmentsResponse?> GetPathSegmentsAsync(string id, string? childNodeId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteVersionAsync(WorkflowDefinitionVersion workflowDefinitionVersion, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SaveWorkflowDefinitionResponse> PublishAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<long> BulkDeleteAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<long> BulkDeleteVersionsAsync(IEnumerable<WorkflowDefinitionVersion> workflowDefinitionVersions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description = null, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description, string? rootActivityTemplateKey, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = null, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> BulkExportDefinitionsAsync(IEnumerable<string> ids, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ExecuteWorkflowResult> ExecuteAsync(string definitionId, ExecuteWorkflowDefinitionRequest? request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
