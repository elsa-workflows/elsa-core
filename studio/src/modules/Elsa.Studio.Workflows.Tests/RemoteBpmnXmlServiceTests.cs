using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Client;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Domain.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="RemoteBpmnXmlService"/>: a document is exchanged as the exact XML text, the <c>ETag</c> travels
/// verbatim, and refusals are classified the same way as the document endpoints'.
/// </summary>
public class RemoteBpmnXmlServiceTests
{
    private const string Xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<bpmn:definitions xmlns:bpmn=\"http://www.omg.org/spec/BPMN/20100524/MODEL\" id=\"D\"><bpmn:process id=\"Commande_traitée\"/></bpmn:definitions>";
    private const string ETag = "\"abc123\"";

    private readonly FakeBpmnXmlApi _api = new();
    private readonly RemoteBpmnXmlService _service;

    public RemoteBpmnXmlServiceTests()
    {
        _service = new(new TestBackendApiClientProvider(_api));
    }

    [Fact]
    public async Task GetXmlAsync_ReturnsTheDocumentVerbatimWithItsETag()
    {
        _api.Response = Respond(HttpStatusCode.OK, Xml, "application/xml", ETag);

        var result = await _service.GetXmlAsync("definition-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(new BpmnXmlRevision(Xml, ETag), result.Success);
        Assert.Equal("definition-1", _api.DefinitionId);
    }

    [Fact]
    public async Task GetXmlAsync_WithoutAnETag_FailsBeforeAnyEditIsMade()
    {
        _api.Response = Respond(HttpStatusCode.OK, Xml, "application/xml");

        var result = await _service.GetXmlAsync("definition-1");

        Assert.Equal(BpmnDocumentFailureReason.MissingETag, result.Failure!.Reason);
        Assert.Contains("CORS", result.Failure.Message);
    }

    [Fact]
    public async Task GetXmlAsync_ClassifiesAStaleSourceByItsCode()
    {
        _api.Response = Refusal(HttpStatusCode.UnprocessableEntity, "The workflow changed since its BPMN source was stored.", BpmnErrorCodes.ExportSourceStale);

        var result = await _service.GetXmlAsync("definition-1");

        Assert.Equal(BpmnDocumentFailureReason.SourceStale, result.Failure!.Reason);
        Assert.Equal("The workflow changed since its BPMN source was stored.", result.Failure.Message);
    }

    [Fact]
    public async Task PutXmlAsync_SendsTheDocumentAsUtf8XmlWithTheETagAndReadsWhatTheSaveDropped()
    {
        _api.Response = Respond(HttpStatusCode.OK, """
            {
              "id": "version-2", "definitionId": "definition-1", "version": 2,
              "analysis": { "processIds": ["Commande_traitée"] },
              "droppedForeignContent": [{ "elementId": "Task_1", "kind": "attribute", "namespace": "urn:acme", "name": "priority" }]
            }
            """, "application/json", "\"def456\"");

        var result = await _service.PutXmlAsync("definition-1", Xml, ETag);

        Assert.Equal(("definition-1", ETag, "application/xml", "utf-8"), (_api.DefinitionId, _api.IfMatch, _api.ContentType, _api.CharSet));
        Assert.Equal(Encoding.UTF8.GetBytes(Xml), _api.Body);
        var saved = result.Success!;
        Assert.Equal(("version-2", "definition-1", 2), (saved.Import.Id, saved.Import.DefinitionId, saved.Import.Version));
        Assert.Equal(["Commande_traitée"], saved.Import.Analysis.ProcessIds);
        Assert.Equal("\"def456\"", saved.ETag);
        var dropped = Assert.Single(saved.DroppedForeignContent);
        Assert.Equal(("Task_1", "attribute", "urn:acme", "priority"), (dropped.ElementId, dropped.Kind, dropped.Namespace, dropped.Name));
    }

    [Theory]
    [InlineData(HttpStatusCode.PreconditionFailed, BpmnErrorCodes.DocumentPreconditionFailed, BpmnDocumentFailureReason.PreconditionFailed)]
    [InlineData(HttpStatusCode.PreconditionRequired, BpmnErrorCodes.DocumentPreconditionRequired, BpmnDocumentFailureReason.PreconditionRequired)]
    [InlineData(HttpStatusCode.UnprocessableEntity, BpmnErrorCodes.ImportBindingInvalid, BpmnDocumentFailureReason.BindingInvalid)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "bpmn.import.duplicate-element-id", BpmnDocumentFailureReason.Unknown)]
    public async Task PutXmlAsync_ClassifiesARefusalByItsCode(HttpStatusCode status, string code, BpmnDocumentFailureReason reason)
    {
        _api.Response = Refusal(status, "Refused.", code);

        var result = await _service.PutXmlAsync("definition-1", Xml, ETag);

        Assert.Equal(reason, result.Failure!.Reason);
        Assert.Equal("Refused.", result.Failure.Message);
    }

    [Fact]
    public async Task CreateAsync_SendsTheNameAndReturnsTheNewDefinitionWithItsETag()
    {
        _api.Response = Respond(HttpStatusCode.Created, """{ "id": "version-1", "definitionId": "new-1", "version": 1, "analysis": {} }""", "application/json", ETag);

        var result = await _service.CreateAsync("Order handling");

        using var request = JsonDocument.Parse(_api.Body);
        Assert.Equal("Order handling", request.RootElement.GetProperty("name").GetString());
        Assert.Equal("application/json", _api.ContentType);
        var created = result.Success!;
        Assert.Equal(("new-1", 1, ETag), (created.Import.DefinitionId, created.Import.Version, created.ETag));
        Assert.Empty(created.DroppedForeignContent);
    }

    [Fact]
    public async Task CreateAsync_ReportsTheServersMessageWhenRefused()
    {
        _api.Response = Respond(HttpStatusCode.BadRequest, """{ "errors": { "generalErrors": ["A name is required to create a BPMN workflow."] } }""", "application/json");

        var result = await _service.CreateAsync(" ");

        Assert.Equal(BpmnDocumentFailureReason.Unknown, result.Failure!.Reason);
        Assert.Equal("A name is required to create a BPMN workflow.", result.Failure.Message);
    }

    private static HttpResponseMessage Respond(HttpStatusCode status, string body, string mediaType, string? eTag = null)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

        if (eTag != null)
            response.Headers.TryAddWithoutValidation("ETag", eTag);

        return response;
    }

    private static HttpResponseMessage Refusal(HttpStatusCode status, string message, string code) =>
        Respond(status, $$"""{ "errors": { "generalErrors": ["{{message}}"] }, "code": "{{code}}" }""", "application/json");

    private class FakeBpmnXmlApi : IBpmnXmlApi
    {
        public HttpResponseMessage? Response { get; set; }
        public string? DefinitionId { get; private set; }
        public string? IfMatch { get; private set; }
        public byte[] Body { get; private set; } = [];
        public string? ContentType { get; private set; }
        public string? CharSet { get; private set; }

        public Task<HttpResponseMessage> GetXmlAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            DefinitionId = definitionId;
            return Task.FromResult(Response ?? throw new InvalidOperationException("No response was set."));
        }

        public async Task<HttpResponseMessage> PutXmlAsync(string definitionId, HttpContent xml, string ifMatch, CancellationToken cancellationToken = default)
        {
            DefinitionId = definitionId;
            IfMatch = ifMatch;
            await RecordAsync(xml, cancellationToken);
            return Response ?? throw new InvalidOperationException("No response was set.");
        }

        public async Task<HttpResponseMessage> CreateAsync(HttpContent request, CancellationToken cancellationToken = default)
        {
            await RecordAsync(request, cancellationToken);
            return Response ?? throw new InvalidOperationException("No response was set.");
        }

        private async Task RecordAsync(HttpContent content, CancellationToken cancellationToken)
        {
            Body = await content.ReadAsByteArrayAsync(cancellationToken);
            ContentType = content.Headers.ContentType?.MediaType;
            CharSet = content.Headers.ContentType?.CharSet;
        }
    }

    private class TestBackendApiClientProvider(IBpmnXmlApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://localhost");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            if (typeof(T) == typeof(IBpmnXmlApi))
                return ValueTask.FromResult((T)api);

            throw new NotSupportedException();
        }
    }
}
