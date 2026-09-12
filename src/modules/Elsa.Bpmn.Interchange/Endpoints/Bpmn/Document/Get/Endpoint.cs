using System.Net.Mime;
using System.Text.Json;
using Elsa.Authorization;
using Bpmn.Model;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document.Get;

/// <summary>
/// Reads a stored workflow definition's BPMN source back out as the library's own <c>bpmnDefinitions</c> JSON
/// document — the shape <c>Endpoints.Bpmn.Document.Put.Put</c> accepts back — rather than as <c>.bpmn</c> XML.
/// </summary>
/// <remarks>
/// A thin wrapper over <see cref="BpmnInterchangeDocumentService.ReadDocument"/>. Studio never holds the whole
/// document as JSON; the round trip this endpoint and <c>Put</c> exist for is what lets an edit made there — a
/// binding (W11), a moved shape (W14) — be written back without a client-side reimplementation of
/// <c>BpmnXmlWriter</c>. Same missing/stale-source refusals as <c>Export</c>; see
/// <see cref="BpmnInterchangeDocumentService"/>'s remarks for what each means.
/// <para>
/// The response body is written with <see cref="BpmnDocumentJsonOptions"/>, not through FastEndpoints' configured
/// serializer — see that type's remarks for why the two disagree on shape.
/// </para>
/// </remarks>
[UsedImplicitly]
internal sealed class Get(IWorkflowDefinitionStore store, BpmnInterchangeDocumentService documentService) : ElsaEndpointWithoutRequest<BpmnDefinitions>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("bpmn/definitions/{definitionId}/document");
        RequirePermission(Elsa.Bpmn.Interchange.Permissions.BpmnPermissions.Definitions, CoreVerbs.View);
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

        await BpmnExportExceptionCascade.RunAsync(
            async () =>
            {
                var document = documentService.ReadDocument(definition);
                var json = JsonSerializer.Serialize(document, BpmnDocumentJsonOptions.Value);
                HttpContext.Response.Headers.ETag = BpmnDocumentETag.From(definition);
                await Send.StringAsync(json, contentType: MediaTypeNames.Application.Json, cancellation: cancellationToken);
            },
            message => AddError(message),
            Send.ErrorsAsync,
            cancellationToken);
    }
}
