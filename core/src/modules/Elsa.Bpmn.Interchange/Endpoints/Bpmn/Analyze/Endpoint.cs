using Elsa.Authorization;
using Bpmn.Interchange;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Analyze;

/// <summary>
/// Reads a <c>.bpmn</c> document and reports the element-scoped Info/Degraded/Dropped findings a read would produce,
/// and the refusal an import of it would raise, without persisting anything.
/// </summary>
/// <remarks>
/// A thin wrapper over <see cref="BpmnInterchangeDocumentService.Analyze"/>, which runs the same code
/// <see cref="Import.Import"/> reads with — see its remarks for why that is what makes this a genuine preview rather
/// than a second opinion that can disagree with the import that follows it.
/// </remarks>
[UsedImplicitly]
internal sealed class Analyze(BpmnInterchangeDocumentService documentService) : ElsaEndpointWithoutRequest<BpmnImportAnalysisModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("bpmn/analyze");
        AllowFileUploads();
        RequirePermission(Elsa.Bpmn.Interchange.Permissions.BpmnPermissions.Definitions, CoreVerbs.View);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (Files.Count != 1)
        {
            AddError("Upload exactly one .bpmn file.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        var xml = await BpmnUploadedFileReader.ReadTextAsync(Files[0], cancellationToken);

        try
        {
            var analysis = documentService.Analyze(xml);
            var refusal = FindRefusal(xml, Query<string>("processId", isRequired: false));
            await Send.OkAsync(BpmnImportAnalysisModel.From(analysis, refusal), cancellationToken);
        }
        catch (BpmnInterchangeException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
        }
        catch (BpmnDuplicateElementIdException exception)
        {
            await BpmnErrorResponse.SendAsync(HttpContext.Response, BpmnImportErrorResponses.DuplicateElementIdResponseFor(exception), cancellationToken);
        }
    }

    private BpmnImportRefusalModel? FindRefusal(string xml, string? processId)
    {
        try
        {
            documentService.Validate(xml, processId);
            return null;
        }
        catch (Exception exception) when (BpmnImportRefusalModel.For(exception) is { } refusal)
        {
            return refusal;
        }
    }
}
