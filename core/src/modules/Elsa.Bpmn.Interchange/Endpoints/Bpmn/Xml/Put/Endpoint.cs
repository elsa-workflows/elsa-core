using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Xml.Put;

/// <summary>
/// Accepts a complete BPMN 2.0 XML document for an existing workflow definition and saves it as that definition's
/// source: the document the BPMN modeler edits. It runs the same import path as the document <c>PUT</c> — analyze,
/// capability, condition and duplicate-id checks, bind, save as a draft — under the same <c>If-Match</c> rules, and
/// keeps the definition's non-BPMN metadata the same way.
/// </summary>
/// <remarks>
/// A thin wrapper over <see cref="BpmnInterchangeDocumentService.ImportXmlAsync"/>. The XML carries every subprocess
/// body, so unlike the document <c>PUT</c> nothing inside a nested scope is restored from the stored source. The
/// response lists the foreign content the stored document had that this one drops (see <see cref="BpmnForeignContent"/>);
/// the save succeeds regardless, since the designer already asks the user to acknowledge such a loss.
/// </remarks>
[UsedImplicitly]
internal sealed class Put(IWorkflowDefinitionStore store, BpmnInterchangeDocumentService documentService) : ElsaEndpointWithoutRequest<Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Put("bpmn/definitions/{definitionId}/xml");
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

        var ifMatch = await BpmnDocumentPrecondition.CheckAsync(HttpContext, definition, cancellationToken);

        if (ifMatch is null)
            return;

        string xml;

        using (var reader = new StreamReader(HttpContext.Request.Body))
            xml = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(xml))
        {
            AddError("The request body must be a BPMN 2.0 XML document.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        var storedProcessId = definition.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceProcessIdCustomPropertyKey, out var processId)
            ? processId
            : null;

        var result = await BpmnImportErrorResponses.RunAsync(
            () => documentService.ImportXmlAsync(xml, definitionId, storedProcessId, cancellationToken, ifMatch),
            HttpContext.Response,
            message => AddError(message),
            Send.ErrorsAsync,
            cancellationToken);

        if (result is null)
            return;

        var persisted = result.ImportResult.WorkflowDefinition;

        // The ETag of the state this PUT saved, not of a reload that could include a later write: see the document PUT.
        HttpContext.Response.Headers.ETag = BpmnDocumentETag.From(persisted);

        // The If-Match check and the compare-and-swap both matched the definition this lookup read, so its stored
        // source is exactly the document this save replaced.
        var storedXml = definition.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey, out var stored) ? stored : null;

        await Send.OkAsync(new Response
        {
            Id = persisted.Id,
            DefinitionId = persisted.DefinitionId,
            Version = persisted.Version,
            Analysis = BpmnImportAnalysisModel.From(result.Analysis),
            DroppedForeignContent = BpmnForeignContent.DroppedBetween(storedXml, xml)
        }, cancellationToken);
    }
}
