using System.Text;
using System.Text.Json;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Client;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class RemoteBpmnXmlService(IBackendApiClientProvider backendApiClientProvider) : IBpmnXmlService
{
    private static readonly JsonSerializerOptions ResponseSerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<Result<BpmnXmlRevision, BpmnDocumentFailure>> GetXmlAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        using var response = await api.GetXmlAsync(definitionId, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new(BpmnDocumentResponses.ClassifyFailure(response, body));

        // Without it no edit could ever be saved: say so now, rather than let the first save fail with a 428 the user
        // cannot act on.
        if (BpmnDocumentResponses.ReadETag(response) is not { } eTag)
            return new(BpmnDocumentResponses.MissingETag());

        return new(new BpmnXmlRevision(body, eTag));
    }

    /// <inheritdoc />
    public async Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> PutXmlAsync(string definitionId, string xml, string eTag, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
        using var response = await api.PutXmlAsync(definitionId, content, eTag, cancellationToken);
        return await ReadSaveResultAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        using var content = new StringContent(JsonSerializer.Serialize(new { name }, ResponseSerializerOptions), Encoding.UTF8, "application/json");
        using var response = await api.CreateAsync(content, cancellationToken);
        return await ReadSaveResultAsync(response, cancellationToken);
    }

    private static async Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> ReadSaveResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new(BpmnDocumentResponses.ClassifyFailure(response, body));

        var saved = JsonSerializer.Deserialize<SaveResponse>(body, ResponseSerializerOptions) ?? new SaveResponse();
        var import = new BpmnImportResultModel { Id = saved.Id, DefinitionId = saved.DefinitionId, Version = saved.Version, Analysis = saved.Analysis };
        return new(new BpmnXmlSaveResult(import, BpmnDocumentResponses.ReadETag(response), saved.DroppedForeignContent ?? []));
    }

    private async Task<IBpmnXmlApi> GetApiAsync(CancellationToken cancellationToken) =>
        await backendApiClientProvider.GetApiAsync<IBpmnXmlApi>(cancellationToken);

    /// <summary>The XML <c>PUT</c> and create response: the <c>bpmn/import</c> shape, plus what a save dropped.</summary>
    private sealed class SaveResponse
    {
        public string Id { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public int Version { get; set; }
        public BpmnImportAnalysisModel Analysis { get; set; } = new();
        public List<BpmnForeignContentModel>? DroppedForeignContent { get; set; }
    }
}
