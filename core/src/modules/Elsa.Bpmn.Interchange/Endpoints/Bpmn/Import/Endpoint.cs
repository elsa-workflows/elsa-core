using Elsa.Authorization;
using Bpmn.Semantics;
using Elsa.Abstractions;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
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
internal sealed class Import(BpmnInterchangeDocumentService documentService, IWorkflowDefinitionStore store) : ElsaEndpoint<Request>
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
        await using var mutationLock = await WorkflowDefinitionMutationLock.AcquireAsync(HttpContext.RequestServices, cancellationToken);

        if (Files.Count != 1)
        {
            AddError("Upload exactly one .bpmn file.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(request.DefinitionId))
        {
            var filter = new WorkflowDefinitionFilter { DefinitionId = request.DefinitionId, VersionOptions = VersionOptions.Latest };
            var order = new WorkflowDefinitionOrder<int>(version => version.Version, OrderDirection.Descending);
            var existing = await store.FindAsync(filter, order, cancellationToken) ?? await store.FindLastVersionAsync(
                new WorkflowDefinitionFilter { DefinitionId = request.DefinitionId }, cancellationToken);
            if (existing != null && !HttpContext.OwnsWorkflowDefinition(existing))
            {
                await Send.ForbiddenAsync(cancellationToken);
                return;
            }
        }

        var xml = await BpmnUploadedFileReader.ReadTextAsync(Files[0], cancellationToken);

        var result = await BpmnImportErrorResponses.RunAsync(
            () => documentService.ImportAsync(xml, request.DefinitionId, request.Name, request.ProcessId, cancellationToken),
            HttpContext.Response,
            message => AddError(message),
            Send.ErrorsAsync,
            cancellationToken);

        if (result is null)
            return;

        var definition = result.ImportResult.WorkflowDefinition;

        await Send.OkAsync(new Response
        {
            Id = definition.Id,
            DefinitionId = definition.DefinitionId,
            Version = definition.Version,
            Analysis = BpmnImportAnalysisModel.From(result)
        }, cancellationToken);
    }
}
