using Bpmn.Interchange;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Export;

/// <summary>Writes a stored workflow definition back out as BPMN 2.0 XML.</summary>
/// <remarks>
/// A thin wrapper over <see cref="BpmnInterchangeDocumentService.Export"/>. See that type's remarks for why this
/// re-reads the original document rather than reconstructing one from the Elsa activity graph the definition runs.
/// </remarks>
[UsedImplicitly]
internal sealed class Export(IWorkflowDefinitionStore store, BpmnInterchangeDocumentService documentService) : ElsaEndpoint<Request>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("bpmn/definitions/{definitionId}/export");
        ConfigurePermissions("read:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var versionOptions = string.IsNullOrWhiteSpace(request.VersionOptions) ? VersionOptions.Latest : VersionOptions.FromString(request.VersionOptions);
        var filter = WorkflowDefinitionHandle.ByDefinitionId(request.DefinitionId, versionOptions).ToFilter();
        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        if (!definition.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey, out var xml) || string.IsNullOrEmpty(xml))
        {
            AddError($"Workflow definition '{request.DefinitionId}' was not imported from a BPMN document, so it cannot be exported as BPMN 2.0 XML.");
            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, cancellationToken);
            return;
        }

        try
        {
            var bytes = documentService.Export(xml);
            await Send.BytesAsync(bytes, $"{request.DefinitionId}.bpmn", "application/xml", cancellation: cancellationToken);
        }
        catch (BpmnInterchangeException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
        }
    }
}
