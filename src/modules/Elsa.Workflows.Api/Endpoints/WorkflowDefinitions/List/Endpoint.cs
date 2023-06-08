using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
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

        var summaries = await FindAsync(request, filter, pageArgs, cancellationToken);
        return new Response(summaries.Items, summaries.TotalCount);
    }

    private WorkflowDefinitionFilter CreateFilter(Request request)
    {
        var versionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : default(VersionOptions?);
        var splitIds = request.DefinitionIds ?? Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm)) {
            return new WorkflowDefinitionFilter { SearchTerm = request.SearchTerm, VersionOptions = versionOptions };
        }

        return splitIds.Any()
            ? new WorkflowDefinitionFilter { DefinitionIds = splitIds, VersionOptions = versionOptions }
            : new WorkflowDefinitionFilter { MaterializerName = request.MaterializerName, VersionOptions = versionOptions };
    }

    private async Task<Page<WorkflowDefinitionSummary>> FindAsync(Request request, WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken)
    {
        request.OrderBy ??= OrderByWorkflowDefinition.Created;

        var direction = request.OrderBy == OrderByWorkflowDefinition.Name ? (request.OrderDirection ?? OrderDirection.Ascending) : (request.OrderDirection ?? OrderDirection.Descending);

        switch (request.OrderBy)
        {
            default:
            {
                    var order = new WorkflowDefinitionOrder<DateTimeOffset>
                    {
                        KeySelector = p => p.CreatedAt,
                        Direction = direction
                    };

                    return await _store.FindSummariesAsync(filter, order, pageArgs, cancellationToken);
                }
            case OrderByWorkflowDefinition.Name:
                {
                    var order = new WorkflowDefinitionOrder<string>
                    {
                        KeySelector = p => p.Name ?? string.Empty,
                        Direction = direction
                    };

                    return await _store.FindSummariesAsync<string>(filter, order, pageArgs, cancellationToken);
                }
        }
    }
}