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
        ConfigurePermissions("write:workflow-definitions");
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

    private void AddCapabilityErrors(BpmnCapabilityException exception)
    {
        var elementIds = string.Join(", ", exception.DrivingElementIds);

        foreach (var capability in BpmnInterchangeDocumentService.IndividualCapabilities)
        {
            if (!exception.Missing.HasFlag(capability))
                continue;

            AddError($"This deployment does not declare the '{capability}' BPMN host capability the document requires. Offending elements: {elementIds}.");
        }
    }
}
