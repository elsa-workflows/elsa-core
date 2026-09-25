using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Client;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class RemoteBpmnInterchangeService(IBackendApiClientProvider backendApiClientProvider) : IBpmnInterchangeService
{
    /// <inheritdoc />
    public async Task<Result<BpmnImportAnalysisModel, ValidationErrors>> AnalyzeAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var file = new StreamPart(content, fileName, "application/xml");

        try
        {
            var analysis = await api.AnalyzeAsync(file, cancellationToken);
            return new(analysis);
        }
        catch (ApiException e)
        {
            return new(e.GetValidationErrors());
        }
    }

    /// <inheritdoc />
    public async Task<Result<BpmnImportResultModel, ValidationErrors>> ImportAsync(
        Stream content,
        string fileName,
        string? definitionId,
        string? name,
        string? processId,
        CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var file = new StreamPart(content, fileName, "application/xml");

        try
        {
            var result = await api.ImportAsync(file, definitionId, name, processId, cancellationToken);
            return new(result);
        }
        catch (ApiException e)
        {
            return new(e.GetValidationErrors());
        }
    }

    /// <inheritdoc />
    public async Task<Result<FileDownload, BpmnExportFailure>> ExportAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.ExportAsync(definitionId, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return new(new FileDownload($"{definitionId}.bpmn", stream));
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new(new BpmnExportFailure(BpmnExportFailureReason.NotFound, "Workflow definition not found."));

        var validationErrors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(body);
        var message = validationErrors?.Errors.FirstOrDefault()?.ErrorMessage
            ?? (string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "The export could not be completed." : body);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            return new(new BpmnExportFailure(ClassifyExportRefusal(validationErrors?.Code), message));

        return new(new BpmnExportFailure(BpmnExportFailureReason.Unknown, message));
    }

    /// <inheritdoc />
    public async Task<Result<BpmnDocumentRevision, BpmnDocumentFailure>> GetDocumentAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        using var response = await api.GetDocumentAsync(definitionId, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new(ClassifyDocumentFailure(response, body));

        // Without it no edit could ever be written back: say so now, rather than let the first save fail with a 428
        // the user cannot act on.
        if (ReadETag(response) is not { } eTag)
            return new(new BpmnDocumentFailure(BpmnDocumentFailureReason.MissingETag, "The server returned the BPMN document without an ETag, so an edit to it could not be saved. If Studio runs on a different origin than the server, the server's CORS policy must expose the ETag header."));

        try
        {
            if (JsonNode.Parse(body) is JsonObject document)
                return new(new BpmnDocumentRevision(document, eTag));
        }
        catch (JsonException)
        {
            // Reported below, the same as any other body that is not a document.
        }

        return new(new BpmnDocumentFailure(BpmnDocumentFailureReason.Unknown, "The server's response is not a BPMN document."));
    }

    /// <inheritdoc />
    public async Task<Result<BpmnDocumentSaveResult, BpmnDocumentFailure>> PutDocumentAsync(string definitionId, JsonObject document, string eTag, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);

        // Written as-is, with plain System.Text.Json defaults: the server binds this body with Bpmn.Model's own
        // (default) options, never with the API-wide conventions a Refit-serialized body would go through.
        using var content = new StringContent(document.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await api.PutDocumentAsync(definitionId, content, eTag, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new(ClassifyDocumentFailure(response, body));

        var import = JsonSerializer.Deserialize<BpmnImportResultModel>(body, ResponseSerializerOptions) ?? new BpmnImportResultModel();
        return new(new BpmnDocumentSaveResult(import, ReadETag(response)));
    }

    /// <summary>
    /// Classifies a document <c>GET</c> or <c>PUT</c> refusal by the envelope's machine-readable <c>code</c> (see
    /// <see cref="BpmnErrorCodes"/>), never by its wording. A response with no code — a plain <c>404</c>, or an older
    /// server — falls back to the HTTP status where the status alone says what happened, and to
    /// <see cref="BpmnDocumentFailureReason.Unknown"/>, carrying the server's message, otherwise.
    /// </summary>
    private static BpmnDocumentFailure ClassifyDocumentFailure(HttpResponseMessage response, string body)
    {
        var errors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(body);
        var message = errors != null
            ? string.Join(" ", errors.Errors.Select(error => error.ErrorMessage))
            : string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? $"The server responded with status {(int)response.StatusCode}." : body;

        var reason = errors?.Code switch
        {
            BpmnErrorCodes.DocumentNotFound => BpmnDocumentFailureReason.NotFound,
            BpmnErrorCodes.DocumentPreconditionRequired => BpmnDocumentFailureReason.PreconditionRequired,
            BpmnErrorCodes.DocumentPreconditionFailed => BpmnDocumentFailureReason.PreconditionFailed,
            BpmnErrorCodes.ImportBindingInvalid => BpmnDocumentFailureReason.BindingInvalid,
            BpmnErrorCodes.ImportCapabilityUnsupported => BpmnDocumentFailureReason.CapabilityUnsupported,
            BpmnErrorCodes.ExportNotImported or BpmnErrorCodes.ExportSourceVersionUnknown => BpmnDocumentFailureReason.NotImportedFromBpmn,
            BpmnErrorCodes.ExportSourceStale => BpmnDocumentFailureReason.SourceStale,
            _ => response.StatusCode switch
            {
                HttpStatusCode.NotFound => BpmnDocumentFailureReason.NotFound,
                HttpStatusCode.PreconditionFailed => BpmnDocumentFailureReason.PreconditionFailed,
                HttpStatusCode.PreconditionRequired => BpmnDocumentFailureReason.PreconditionRequired,
                _ => BpmnDocumentFailureReason.Unknown
            }
        };

        var capabilityRefusal = reason == BpmnDocumentFailureReason.CapabilityUnsupported ? BpmnCapabilityRefusal.FromData(errors?.Data) : null;
        return new(reason, message, capabilityRefusal);
    }

    /// <summary>The <c>ETag</c> header verbatim, quotes included, or <see langword="null"/> when there is none.</summary>
    private static string? ReadETag(HttpResponseMessage response) =>
        response.Headers.TryGetValues("ETag", out var values) ? values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) : null;

    /// <summary>
    /// Classifies the 422 refusals <c>BpmnInterchangeDocumentService.Export(WorkflowDefinition)</c> raises by their
    /// machine-readable <paramref name="code"/> (see <see cref="BpmnErrorCodes"/>), rather than by matching a phrase
    /// in the message, so a rewording of the message never breaks this classification. A <see langword="null"/> or
    /// unrecognized code — including an older server that does not send one yet — falls back to
    /// <see cref="BpmnExportFailureReason.Unknown"/>, which shows the server's own message.
    /// </summary>
    private static BpmnExportFailureReason ClassifyExportRefusal(string? code) => code switch
    {
        BpmnErrorCodes.ExportNotImported => BpmnExportFailureReason.NotImportedFromBpmn,
        // Not reachable through Import or the document PUT themselves (both always record a source version
        // alongside the source text); Studio has no wording of its own for this case, distinct from
        // ExportNotImported's, so it maps to the same "not imported" message.
        BpmnErrorCodes.ExportSourceVersionUnknown => BpmnExportFailureReason.NotImportedFromBpmn,
        BpmnErrorCodes.ExportSourceStale => BpmnExportFailureReason.DefinitionChangedSinceImport,
        _ => BpmnExportFailureReason.Unknown
    };

    /// <summary>
    /// The document <c>PUT</c>'s success body is an ordinary API response (the <c>bpmn/import</c> response shape), written
    /// with the server's API-wide camelCase conventions, unlike the document itself.
    /// </summary>
    private static readonly JsonSerializerOptions ResponseSerializerOptions = new(JsonSerializerDefaults.Web);

    private async Task<IBpmnInterchangeApi> GetApiAsync(CancellationToken cancellationToken = default) =>
        await backendApiClientProvider.GetApiAsync<IBpmnInterchangeApi>(cancellationToken);
}
