using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Http.Services;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

/// <summary>
/// An API endpoint that executes a given workflow definition.
/// </summary>
[PublicAPI]
public class Execute : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IHttpBookmarkProcessor _httpBookmarkProcessor;

    /// <inheritdoc />
    public Execute(IWorkflowDefinitionStore store, IWorkflowRuntime workflowRuntime, IHttpBookmarkProcessor httpBookmarkProcessor)
    {
        _store = store;
        _workflowRuntime = workflowRuntime;
        _httpBookmarkProcessor = httpBookmarkProcessor;
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

        // Resume any HTTP bookmarks.
        await _httpBookmarkProcessor.ProcessBookmarks(new[] { result }, correlationId, default, cancellationToken);

        if (!HttpContext.Response.HasStarted)
            await SendOkAsync(new Response(result.InstanceId), cancellationToken);
    }
}