using Elsa.Authorization;
using Bpmn.Interchange;
using Bpmn.Semantics;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Import;

/// <summary>
/// Reads a <c>.bpmn</c> document and persists it as a workflow definition whose root is the <see cref="Elsa.Bpmn.Activities.BpmnProcess"/>
/// scope <c>BpmnWorkBinder</c> builds for it.
/// </summary>
/// <remarks>
/// A thin wrapper over <see cref="BpmnInterchangeDocumentService.ImportAsync"/>. Its remarks explain why capability
/// refusal is surfaced here rather than left to first execution, and why nothing beyond the source XML is persisted
/// alongside the bound activity graph.
/// </remarks>
[UsedImplicitly]
internal sealed class Import(BpmnInterchangeDocumentService documentService) : ElsaEndpoint<Request>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("bpmn/import");
        AllowFileUploads();
        RequirePermission(Elsa.Bpmn.Interchange.Permissions.BpmnPermissions.Definitions, CoreVerbs.Write);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (Files.Count != 1)
        {
            AddError("Upload exactly one .bpmn file.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        var xml = await BpmnUploadedFileReader.ReadTextAsync(Files[0], cancellationToken);

        BpmnDocumentImportResult result;

        try
        {
            result = await documentService.ImportAsync(xml, request.DefinitionId, request.Name, request.ProcessId, cancellationToken);
        }
        catch (BpmnInterchangeException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }
        catch (BpmnBindingException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, cancellationToken);
            return;
        }
        catch (BpmnCapabilityException exception)
        {
            AddCapabilityErrors(exception);
            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, cancellationToken);
            return;
        }

        if (!result.ImportResult.Succeeded)
        {
            foreach (var validationError in result.ImportResult.ValidationErrors)
                AddError(validationError.Message);

            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        var definition = result.ImportResult.WorkflowDefinition;

        await Send.OkAsync(new Response
        {
            Id = definition.Id,
            DefinitionId = definition.DefinitionId,
            Version = definition.Version,
            Analysis = BpmnImportAnalysisModel.From(result.Analysis)
        }, cancellationToken);
    }

    /// <remarks>
    /// <see cref="BpmnCapabilityException"/> carries <see cref="BpmnCapabilityException.DrivingElementIds"/> as a
    /// single flat list, already unioned across every missing capability — it does not say which element drove which
    /// capability (unlike <c>BpmnCapabilityRequirements.DrivingElementIds</c>, which is per-capability, but that type
    /// is gone by the time this catch clause sees the exception). Attributing the full, unioned list to each
    /// capability individually would put elements next to a capability they may have nothing to do with, so this
    /// reports the missing capabilities together with the combined element list once, rather than repeating it.
    /// </remarks>
    private void AddCapabilityErrors(BpmnCapabilityException exception)
    {
        var missingCapabilities = string.Join(", ", BpmnInterchangeDocumentService.IndividualCapabilities.Where(capability => exception.Missing.HasFlag(capability)));
        var elementIds = string.Join(", ", exception.DrivingElementIds);

        AddError(
            $"This deployment does not declare the following BPMN host capabilities the document requires: {missingCapabilities}. "
            + $"Offending elements (combined across all missing capabilities above, not attributable to any one of them): {elementIds}.");
    }
}
