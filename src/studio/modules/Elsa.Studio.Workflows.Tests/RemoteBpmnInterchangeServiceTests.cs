using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Client;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.Tests.Support;
using Refit;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="RemoteBpmnInterchangeService"/>'s error mapping: the server's 400/422 payloads carry the
/// message a user needs to see (capability names, offending element ids, or one of the export refusals), and the
/// export refusals are told apart by the envelope's machine-readable <c>code</c> (see <see cref="BpmnErrorCodes"/>)
/// rather than by matching the message text, so a rewording of the message never breaks this classification. An
/// unrecognized code, or a body with no code at all (an older server), falls back to showing the server's own
/// message under <see cref="BpmnExportFailureReason.Unknown"/>.
/// </summary>
public class RemoteBpmnInterchangeServiceTests : IDisposable
{
    private readonly FakeBpmnInterchangeApi _api = new();
    private readonly RemoteBpmnInterchangeService _service;

    public RemoteBpmnInterchangeServiceTests()
    {
        _service = new(new TestBackendApiClientProvider(_api));
    }

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task AnalyzeAsync_ReturnsTheServersErrorMessage_WhenTheApiThrows()
    {
        _api.AnalyzeException = await CreateApiExceptionAsync(HttpStatusCode.BadRequest, """
        {
          "errors": {
            "generalErrors": ["Upload exactly one .bpmn file."]
          }
        }
        """);

        using var stream = new MemoryStream();
        var result = await _service.AnalyzeAsync(stream, "process.bpmn");

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Failure!.Errors);
        Assert.Equal("Upload exactly one .bpmn file.", error.ErrorMessage);
    }

    [Fact]
    public async Task ImportAsync_ReturnsTheCapabilityRefusalMessageAndCodedData_On422()
    {
        const string message =
            "This deployment does not declare the following BPMN host capabilities the document requires: ScopeSignalling. "
            + "Offending elements (combined across all missing capabilities above, not attributable to any one of them): Gateway_1.";

        _api.ImportException = await CreateApiExceptionAsync(HttpStatusCode.UnprocessableEntity, $$"""
        {
          "errors": {
            "generalErrors": ["{{message}}"]
          },
          "code": "{{BpmnErrorCodes.ImportCapabilityUnsupported}}",
          "data": { "capabilities": ["ScopeSignalling"], "elementIds": ["Gateway_1"] }
        }
        """);

        using var stream = new MemoryStream();
        var result = await _service.ImportAsync(stream, "process.bpmn", definitionId: null, name: null, processId: null);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Failure!.Errors);
        Assert.Equal(message, error.ErrorMessage);
        Assert.Equal(BpmnErrorCodes.ImportCapabilityUnsupported, result.Failure.Code);
        var refusal = BpmnCapabilityRefusal.FromData(result.Failure.Data);
        Assert.Equal(["ScopeSignalling"], refusal!.CapabilityNames);
        Assert.Equal(["Gateway_1"], refusal.ElementIds);
    }

    [Fact]
    public async Task ExportAsync_ReturnsNotFound_WhenTheDefinitionDoesNotExist()
    {
        _api.ExportResponse = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("") };

        var result = await _service.ExportAsync("missing-definition");

        Assert.True(result.IsFailed);
        Assert.Equal(BpmnExportFailureReason.NotFound, result.Failure!.Reason);
    }

    [Fact]
    public async Task ExportAsync_ClassifiesNotImportedFromBpmn()
    {
        const string message = "Workflow definition 'wf-1' does not currently carry BPMN source, so it cannot be exported as BPMN 2.0 XML.";
        _api.ExportResponse = UnprocessableEntity(message, BpmnErrorCodes.ExportNotImported);

        var result = await _service.ExportAsync("wf-1");

        Assert.True(result.IsFailed);
        Assert.Equal(BpmnExportFailureReason.NotImportedFromBpmn, result.Failure!.Reason);
        Assert.Equal(message, result.Failure.Message);
    }

    [Fact]
    public async Task ExportAsync_ClassifiesDefinitionChangedSinceImport()
    {
        const string message = "Workflow definition 'wf-1' has changed since it was imported from BPMN (imported at version 1, currently at version 2).";
        _api.ExportResponse = UnprocessableEntity(message, BpmnErrorCodes.ExportSourceStale);

        var result = await _service.ExportAsync("wf-1");

        Assert.True(result.IsFailed);
        Assert.Equal(BpmnExportFailureReason.DefinitionChangedSinceImport, result.Failure!.Reason);
    }

    [Fact]
    public async Task ExportAsync_ClassifiesSourceVersionUnknownAsNotImportedFromBpmn()
    {
        // Studio has no wording of its own distinct from ExportNotImported's for this rarely-reachable case (see
        // RemoteBpmnInterchangeService.ClassifyExportRefusal's remarks), so it maps to the same reason.
        const string message = "Workflow definition 'wf-1' carries BPMN source but not the definition version it was recorded against.";
        _api.ExportResponse = UnprocessableEntity(message, BpmnErrorCodes.ExportSourceVersionUnknown);

        var result = await _service.ExportAsync("wf-1");

        Assert.True(result.IsFailed);
        Assert.Equal(BpmnExportFailureReason.NotImportedFromBpmn, result.Failure!.Reason);
    }

    [Fact]
    public async Task ExportAsync_FallsBackToUnknown_ForAnUnrecognizedCode()
    {
        const string message = "Something else went wrong.";
        _api.ExportResponse = UnprocessableEntity(message, "bpmn.export.some-future-code");

        var result = await _service.ExportAsync("wf-1");

        Assert.True(result.IsFailed);
        Assert.Equal(BpmnExportFailureReason.Unknown, result.Failure!.Reason);
        Assert.Equal(message, result.Failure.Message);
    }

    [Fact]
    public async Task ExportAsync_FallsBackToUnknown_ForA422WithNoCode()
    {
        // An older server that has not been upgraded to send `code` yet; the failure still surfaces the message,
        // just without a specific reason.
        const string message = "Workflow definition 'wf-1' does not currently carry BPMN source, so it cannot be exported as BPMN 2.0 XML.";
        _api.ExportResponse = UnprocessableEntity(message, code: null);

        var result = await _service.ExportAsync("wf-1");

        Assert.True(result.IsFailed);
        Assert.Equal(BpmnExportFailureReason.Unknown, result.Failure!.Reason);
        Assert.Equal(message, result.Failure.Message);
    }

    [Fact]
    public async Task ExportAsync_ReturnsTheDownload_OnSuccess()
    {
        const string xml = "<definitions />";
        _api.ExportResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes(xml))
        };

        var result = await _service.ExportAsync("wf-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("wf-1.bpmn", result.Success!.FileName);
        using var reader = new StreamReader(result.Success.Content);
        Assert.Equal(xml, await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task GetDocumentAsync_ReturnsTheDocumentAndItsETagVerbatim()
    {
        _api.GetDocumentResponse = Json(HttpStatusCode.OK, BpmnDocumentFixtures.Document().ToJsonString(), eTag: "\"ABC123\"");

        var result = await _service.GetDocumentAsync("wf-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("\"ABC123\"", result.Success!.ETag);
        Assert.True(JsonNode.DeepEquals(BpmnDocumentFixtures.Document(), result.Success.Document));
    }

    /// <summary>
    /// A document read without its ETag could never be written back (the PUT would be refused 428); across origins that
    /// is what a CORS policy that does not expose the header produces, so it is reported as such, not as success.
    /// </summary>
    [Fact]
    public async Task GetDocumentAsync_RefusesADocumentThatCameWithoutAnETag()
    {
        _api.GetDocumentResponse = Json(HttpStatusCode.OK, BpmnDocumentFixtures.Document().ToJsonString());

        var result = await _service.GetDocumentAsync("wf-1");

        Assert.Equal(BpmnDocumentFailureReason.MissingETag, result.Failure!.Reason);
    }

    [Fact]
    public async Task GetDocumentAsync_ClassifiesAStaleDocumentByItsCode()
    {
        const string message = "Workflow definition 'wf-1' has changed since it was imported from BPMN.";
        _api.GetDocumentResponse = UnprocessableEntity(message, BpmnErrorCodes.ExportSourceStale);

        var result = await _service.GetDocumentAsync("wf-1");

        Assert.Equal(BpmnDocumentFailureReason.SourceStale, result.Failure!.Reason);
        Assert.Equal(message, result.Failure.Message);
    }

    [Fact]
    public async Task PutDocumentAsync_SendsTheDocumentAsPlainJson_WithTheETagAsIfMatch()
    {
        var document = BpmnDocumentFixtures.Document();
        _api.PutDocumentResponse = Json(HttpStatusCode.OK, """{"id":"v2","definitionId":"wf-1","version":2,"analysis":{"processIds":["order-process"]}}""", eTag: "\"NEW\"");

        var result = await _service.PutDocumentAsync("wf-1", document, "\"OLD\"");

        Assert.Equal(("wf-1", "\"OLD\"", "application/json"), (_api.PutDefinitionId, _api.PutIfMatch, _api.PutContentType));
        Assert.Equal(document.ToJsonString(), _api.PutBody);
        Assert.Equal("\"NEW\"", result.Success!.ETag);
        Assert.Equal(("v2", 2, "order-process"), (result.Success.Import.Id, result.Success.Import.Version, result.Success.Import.Analysis.ProcessIds.Single()));
    }

    [Theory]
    [InlineData(HttpStatusCode.PreconditionFailed, BpmnErrorCodes.DocumentPreconditionFailed, BpmnDocumentFailureReason.PreconditionFailed)]
    [InlineData(HttpStatusCode.PreconditionRequired, BpmnErrorCodes.DocumentPreconditionRequired, BpmnDocumentFailureReason.PreconditionRequired)]
    [InlineData(HttpStatusCode.UnprocessableEntity, BpmnErrorCodes.ImportBindingInvalid, BpmnDocumentFailureReason.BindingInvalid)]
    [InlineData(HttpStatusCode.NotFound, BpmnErrorCodes.DocumentNotFound, BpmnDocumentFailureReason.NotFound)]
    [InlineData(HttpStatusCode.PreconditionFailed, null, BpmnDocumentFailureReason.PreconditionFailed)]
    [InlineData(HttpStatusCode.NotFound, null, BpmnDocumentFailureReason.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity, null, BpmnDocumentFailureReason.Unknown)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "bpmn.import.some-future-code", BpmnDocumentFailureReason.Unknown)]
    public async Task PutDocumentAsync_ClassifiesARefusalByItsCode_FallingBackToTheStatus(HttpStatusCode status, string? code, BpmnDocumentFailureReason expected)
    {
        const string message = "The server's own words.";
        _api.PutDocumentResponse = Refusal(status, message, code);

        var result = await _service.PutDocumentAsync("wf-1", BpmnDocumentFixtures.Document(), "\"OLD\"");

        Assert.Equal(expected, result.Failure!.Reason);
        Assert.Equal(message, result.Failure.Message);
    }

    [Fact]
    public async Task PutDocumentAsync_CarriesTheCapabilityRefusalsData()
    {
        _api.PutDocumentResponse = new(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent($$"""
            {
              "errors": { "generalErrors": ["Missing capabilities."] },
              "code": "{{BpmnErrorCodes.ImportCapabilityUnsupported}}",
              "data": { "capabilities": ["ScopeSignalling"], "elementIds": ["Gateway_1"] }
            }
            """)
        };

        var result = await _service.PutDocumentAsync("wf-1", BpmnDocumentFixtures.Document(), "\"OLD\"");

        Assert.Equal(BpmnDocumentFailureReason.CapabilityUnsupported, result.Failure!.Reason);
        Assert.Equal(["Gateway_1"], result.Failure.CapabilityRefusal!.ElementIds);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body, string? eTag = null)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        if (eTag != null)
            response.Headers.ETag = new(eTag);

        return response;
    }

    private static HttpResponseMessage Refusal(HttpStatusCode status, string message, string? code)
    {
        var codeProperty = code is null ? "" : $$""", "code": "{{code}}" """;
        return new(status) { Content = new StringContent($$"""{ "errors": { "generalErrors": ["{{message}}"] }{{codeProperty}} }""") };
    }

    private static HttpResponseMessage UnprocessableEntity(string message, string? code)
    {
        var codeProperty = code is null ? "" : $$""", "code": "{{code}}" """;

        return new(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent($$"""
            {
              "errors": {
                "generalErrors": ["{{message}}"]
              }{{codeProperty}}
            }
            """)
        };
    }

    private static async Task<ApiException> CreateApiExceptionAsync(HttpStatusCode statusCode, string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/bpmn/analyze");
        using var response = new HttpResponseMessage(statusCode)
        {
            RequestMessage = request,
            Content = new StringContent(content)
        };

        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private class FakeBpmnInterchangeApi : IBpmnInterchangeApi, IDisposable
    {
        private readonly HttpResponseMessage _defaultExportResponse = new(HttpStatusCode.OK) { Content = new StringContent("") };

        public ApiException? AnalyzeException { get; set; }
        public ApiException? ImportException { get; set; }
        public HttpResponseMessage? ExportResponse { get; set; }
        public HttpResponseMessage? GetDocumentResponse { get; set; }
        public HttpResponseMessage? PutDocumentResponse { get; set; }
        public string? PutDefinitionId { get; private set; }
        public string? PutIfMatch { get; private set; }
        public string? PutBody { get; private set; }
        public string? PutContentType { get; private set; }

        public Task<BpmnImportAnalysisModel> AnalyzeAsync(StreamPart file, CancellationToken cancellationToken = default) =>
            AnalyzeException != null ? throw AnalyzeException : Task.FromResult(new BpmnImportAnalysisModel());

        public Task<BpmnImportResultModel> ImportAsync(StreamPart file, string? definitionId, string? name, string? processId, CancellationToken cancellationToken = default) =>
            ImportException != null ? throw ImportException : Task.FromResult(new BpmnImportResultModel());

        public Task<HttpResponseMessage> ExportAsync(string definitionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExportResponse ?? _defaultExportResponse);

        public Task<HttpResponseMessage> GetDocumentAsync(string definitionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(GetDocumentResponse ?? throw new InvalidOperationException("No document response was set."));

        public async Task<HttpResponseMessage> PutDocumentAsync(string definitionId, HttpContent document, string ifMatch, CancellationToken cancellationToken = default)
        {
            PutDefinitionId = definitionId;
            PutIfMatch = ifMatch;
            PutBody = await document.ReadAsStringAsync(cancellationToken);
            PutContentType = document.Headers.ContentType?.MediaType;
            return PutDocumentResponse ?? throw new InvalidOperationException("No document response was set.");
        }

        public void Dispose()
        {
            _defaultExportResponse.Dispose();
            ExportResponse?.Dispose();
        }
    }

    private class TestBackendApiClientProvider(IBpmnInterchangeApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://localhost");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            if (typeof(T) == typeof(IBpmnInterchangeApi))
                return ValueTask.FromResult((T)api);

            throw new NotSupportedException();
        }
    }
}
