using Elsa.Authorization;
using Bpmn.Interchange;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Export;

/// <summary>Writes a stored workflow definition back out as BPMN 2.0 XML.</summary>
/// <remarks>
/// A thin wrapper over <see cref="BpmnInterchangeDocumentService.Export(Elsa.Workflows.Management.Entities.WorkflowDefinition)"/>.
/// See that type's remarks for why this re-reads the original document rather than reconstructing one from the Elsa
/// activity graph the definition runs, and for why a missing or stale source is refused rather than exported anyway.
/// </remarks>
[UsedImplicitly]
internal sealed class Export(IWorkflowDefinitionStore store, BpmnInterchangeDocumentService documentService) : ElsaEndpoint<Request>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("bpmn/definitions/{definitionId}/export");
        RequirePermission(Elsa.Bpmn.Interchange.Permissions.BpmnPermissions.Definitions, CoreVerbs.View);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        VersionOptions versionOptions;

        if (string.IsNullOrWhiteSpace(request.VersionOptions))
        {
            versionOptions = VersionOptions.Latest;
        }
        else
        {
            try
            {
                // VersionOptions.TryParse always returns true and still throws through FromString for a value it
                // cannot make sense of, so it buys nothing over calling FromString directly — catch what it can
                // actually throw instead: a non-numeric or out-of-range value that is not one of its named options.
                versionOptions = VersionOptions.FromString(request.VersionOptions);
            }
            catch (Exception exception) when (exception is FormatException or OverflowException)
            {
                AddError(
                    $"'{request.VersionOptions}' is not a valid version. Specify a whole number, or one of: AllVersions, Draft, Latest, Published, "
                    + "LatestOrPublished, LatestAndPublished.");
                await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
                return;
            }
        }

        var filter = WorkflowDefinitionHandle.ByDefinitionId(request.DefinitionId, versionOptions).ToFilter();
        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        try
        {
            var bytes = documentService.Export(definition);
            await Send.BytesAsync(bytes, $"{request.DefinitionId}.bpmn", "application/xml", cancellation: cancellationToken);
        }
        catch (BpmnExportUnavailableException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, cancellationToken);
        }
        catch (BpmnInterchangeException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
        }
    }
}
