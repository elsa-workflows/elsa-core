using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.List;

[PublicAPI]
internal class List : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _store;

    public List(IWorkflowDefinitionStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/workflow-definitions");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = new PageArgs(request.Page, request.PageSize);
        var filter = CreateFilter(request);
        var summaries = await _store.FindSummariesAsync(filter, pageArgs, cancellationToken);
        return new Response(summaries.Items, summaries.TotalCount);
    }

    private WorkflowDefinitionFilter CreateFilter(Request request)
    {
        var versionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : default(VersionOptions?);
        var splitIds = request.DefinitionIds ?? Array.Empty<string>();

        return splitIds.Any()
            ? new WorkflowDefinitionFilter { DefinitionIds = splitIds, VersionOptions = versionOptions }
            : new WorkflowDefinitionFilter { MaterializerName = request.MaterializerName, VersionOptions = versionOptions };
    }
}