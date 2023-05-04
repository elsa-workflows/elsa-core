using System.Net.Mime;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Http.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

/// <summary>
/// An API endpoint that executes a given workflow definition.
/// </summary>
[PublicAPI]
internal class Execute : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IHttpBookmarkProcessor _httpBookmarkProcessor;
    private IApiSerializer _apiSerializer;

    /// <inheritdoc />
    public Execute(IWorkflowDefinitionStore store, IWorkflowRuntime workflowRuntime, IHttpBookmarkProcessor httpBookmarkProcessor, IApiSerializer apiSerializer)
    {
        _store = store;
        _workflowRuntime = workflowRuntime;
        _httpBookmarkProcessor = httpBookmarkProcessor;
        _apiSerializer = apiSerializer;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/workflow-definitions/{definitionId}/execute");
        ConfigurePermissions("exec:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var definitionId = request.DefinitionId;
        var exists = await _store.AnyAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Published }, cancellationToken);

        if (!exists)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var correlationId = request.CorrelationId;
        var input = (IDictionary<string, object>?)request.Input;
        var startWorkflowOptions = new StartWorkflowRuntimeOptions(correlationId, input, VersionOptions.Published);
        var result = await _workflowRuntime.StartWorkflowAsync(definitionId, startWorkflowOptions, cancellationToken);

        // If a workflow fault occurred, respond appropriately with a 500 internal server error.
        if (result.SubStatus == WorkflowSubStatus.Faulted)
        {
            var workflowState = await _workflowRuntime.ExportWorkflowStateAsync(result.WorkflowInstanceId, cancellationToken);
            await HandleFaultAsync(workflowState!, cancellationToken);
        }
        else
        {
            // Resume any HTTP bookmarks.
            await _httpBookmarkProcessor.ProcessBookmarks(new[] { result }, correlationId, default, cancellationToken);

            var workflowState = await _workflowRuntime.ExportWorkflowStateAsync(result.WorkflowInstanceId, cancellationToken);

            if (workflowState!.SubStatus == WorkflowSubStatus.Faulted)
            {
                await HandleFaultAsync(workflowState, cancellationToken);
                return;
            }

            if (!HttpContext.Response.HasStarted)
                await SendOkAsync(new Response(workflowState), cancellationToken);
        }
    }

    private async Task HandleFaultAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var faultedResponse = _apiSerializer.Serialize(new
        {
            errorMessage = $"Workflow faulted with error: {workflowState.Fault!.Message}",
            workflowState = workflowState
        });

        HttpContext.Response.ContentType = MediaTypeNames.Application.Json;
        HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await HttpContext.Response.WriteAsync(faultedResponse, cancellationToken);
    }
}