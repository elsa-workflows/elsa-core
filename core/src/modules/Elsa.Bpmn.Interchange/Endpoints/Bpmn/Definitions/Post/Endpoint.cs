using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document;
using Elsa.Bpmn.Interchange.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using ImportResponse = Elsa.Bpmn.Interchange.Endpoints.Bpmn.Import.Response;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Definitions.Post;

/// <summary>
/// Creates a BPMN workflow from scratch: a new draft workflow definition holding a minimal process, ready to open in
/// the modeler and save through <c>PUT bpmn/definitions/{definitionId}/xml</c>.
/// </summary>
/// <remarks>
/// A thin wrapper over <see cref="BpmnInterchangeDocumentService.CreateAsync"/>. Returns <c>201 Created</c> with the
/// <c>Import</c> response shape, a <c>Location</c> header naming the new definition's XML, and the <c>ETag</c> the
/// first XML <c>PUT</c> needs, so a client does not have to read the document back before editing it.
/// </remarks>
[UsedImplicitly]
internal sealed class Post(BpmnInterchangeDocumentService documentService) : ElsaEndpoint<Request, ImportResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("bpmn/definitions");
        RequirePermission(Elsa.Bpmn.Interchange.Permissions.BpmnPermissions.Definitions, CoreVerbs.Write);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            AddError("A name is required to create a BPMN workflow.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        var result = await BpmnImportErrorResponses.RunAsync(
            () => documentService.CreateAsync(name, cancellationToken),
            HttpContext.Response,
            message => AddError(message),
            Send.ErrorsAsync,
            cancellationToken);

        if (result is null)
        {
            return;
        }

        var definition = result.ImportResult.WorkflowDefinition;
        HttpContext.Response.Headers.ETag = BpmnDocumentETag.From(definition);

        await Send.CreatedAtAsync<Xml.Get.Get>(
            new { definitionId = definition.DefinitionId },
            new ImportResponse
            {
                Id = definition.Id,
                DefinitionId = definition.DefinitionId,
                Version = definition.Version,
                Analysis = BpmnImportAnalysisModel.From(result.Analysis)
            },
            cancellation: cancellationToken);
    }
}
