using System.Text.Json;
using Elsa.Authorization;
using Bpmn.Model;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn.Import;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document.Put;

/// <summary>
/// Accepts the whole <c>bpmnDefinitions</c> JSON document for an existing workflow definition, writes it back out as
/// BPMN 2.0 XML, and imports it through the same path <c>Endpoints.Bpmn.Import.Import</c> runs — analyze, capability
/// check, bind, persist as a draft, refresh the stored source. A published version is never edited in place: like
/// <c>Import</c>, this always produces a new draft.
/// </summary>
/// <remarks>
/// A thin wrapper over <see cref="BpmnInterchangeDocumentService.ImportDocumentAsync"/>. The request body is bound as
/// a raw string and deserialized explicitly with <see cref="BpmnDocumentJsonOptions"/>, not through FastEndpoints'
/// configured serializer — see that type's remarks for why the two disagree on shape, and
/// <c>Endpoints.Bpmn.Document.Get.Get</c> for the read side of this round trip.
/// </remarks>
[UsedImplicitly]
internal sealed class Put(IWorkflowDefinitionStore store, BpmnInterchangeDocumentService documentService) : ElsaEndpointWithoutRequest<Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Put("bpmn/definitions/{definitionId}/document");
        RequirePermission(Elsa.Bpmn.Interchange.Permissions.BpmnPermissions.Definitions, CoreVerbs.Write);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var definitionId = Route<string>("definitionId")!;
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        // Optimistic concurrency: a client that GETs the document, then PUTs it back after someone else has written the
        // definition in between, must not silently replace that intervening write. The client is required to carry the
        // ETag its GET returned as If-Match. A missing header cannot express "I know what I'm overwriting" at all, and
        // neither can "*", which matches whatever is stored — so both are refused as 428 rather than honoured. Anything
        // else must be exactly the current strong ETag (a weak W/ tag or a list never is), or it proves the client's copy
        // is no longer current. All of this is checked before any import work runs or anything is persisted.
        var ifMatch = HttpContext.Request.Headers.IfMatch.ToString().Trim();

        if (ifMatch is "" or "*")
        {
            AddError("An If-Match header carrying the ETag from a prior GET of this document is required to PUT it back, so an intervening edit is not silently overwritten. The wildcard \"*\" is not accepted.");
            await Send.ErrorsAsync(StatusCodes.Status428PreconditionRequired, cancellationToken);
            return;
        }

        if (!string.Equals(ifMatch, BpmnDocumentETag.From(definition), StringComparison.Ordinal))
        {
            AddError("The workflow definition has been written since the ETag in If-Match was issued. GET the document again, reapply the edit, and PUT it with the new ETag.");
            await Send.ErrorsAsync(StatusCodes.Status412PreconditionFailed, cancellationToken);
            return;
        }

        string body;

        using (var reader = new StreamReader(HttpContext.Request.Body))
            body = await reader.ReadToEndAsync(cancellationToken);

        BpmnDefinitions? document;

        try
        {
            document = JsonSerializer.Deserialize<BpmnDefinitions>(body, BpmnDocumentJsonOptions.Value);
        }
        catch (JsonException exception)
        {
            AddError($"The request body is not a valid BPMN document: {exception.Message}");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        if (document is null)
        {
            AddError("The request body must be a BPMN document, not JSON null.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        // The process this definition was imported from, when the document that produced it declared more than one:
        // reused here so a multi-process document keeps importing the same process on every edit, without asking the
        // caller to say so again. See BpmnInterchangeDocumentService.SourceProcessIdCustomPropertyKey.
        var processId = definition.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceProcessIdCustomPropertyKey, out var storedProcessId)
            ? storedProcessId
            : null;

        var result = await BpmnImportExceptionCascade.RunAsync(
            () => documentService.ImportDocumentAsync(document, definitionId, processId, cancellationToken),
            message => AddError(message),
            Send.ErrorsAsync,
            cancellationToken);

        if (result is null)
            return;

        var persisted = result.ImportResult.WorkflowDefinition;

        // The definition exactly as ImportDocumentAsync's final save wrote it, so the ETag names the state this PUT
        // produced. Reloading it from the store instead could pick up a write that landed after that save and hand the
        // client a validator for content it never saw, letting its next PUT overwrite that write silently.
        HttpContext.Response.Headers.ETag = BpmnDocumentETag.From(persisted);

        await Send.OkAsync(new Response
        {
            Id = persisted.Id,
            DefinitionId = persisted.DefinitionId,
            Version = persisted.Version,
            Analysis = BpmnImportAnalysisModel.From(result.Analysis)
        }, cancellationToken);
    }
}
