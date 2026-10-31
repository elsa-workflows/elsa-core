using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Xml.Get;

/// <summary>
/// Reads a stored workflow definition's BPMN source back out as BPMN 2.0 XML, exactly as stored, with the
/// <c>ETag</c> that <c>Endpoints.Bpmn.Xml.Put.Put</c> requires to save it back.
/// </summary>
/// <remarks>
/// A thin wrapper over <see cref="BpmnInterchangeDocumentService.ReadSourceXml"/>. Unlike <c>Export</c>, it reads the
/// latest version only and returns the stored text rather than writing it again through the library, so a modeler
/// edits exactly what the <c>ETag</c> covers. Same missing/stale-source refusals as <c>Export</c>.
/// </remarks>
[UsedImplicitly]
internal sealed class Get(IWorkflowDefinitionStore store, BpmnInterchangeDocumentService documentService) : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("bpmn/definitions/{definitionId}/xml");
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

        await BpmnExportErrorResponses.RunAsync(
            async () =>
            {
                var xml = documentService.ReadSourceXml(definition);
                HttpContext.Response.Headers.ETag = BpmnDocumentETag.From(definition);
                await Send.StringAsync(xml, contentType: "application/xml", cancellation: cancellationToken);
            },
            HttpContext.Response,
            message => AddError(message),
            Send.ErrorsAsync,
            cancellationToken);
    }
}
