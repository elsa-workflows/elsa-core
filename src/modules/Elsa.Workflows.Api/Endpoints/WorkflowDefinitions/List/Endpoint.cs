using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Framework.Entities;
using Elsa.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.List;

[PublicAPI]
internal class List(IWorkflowDefinitionStore store, IWorkflowDefinitionLinker linker) : ElsaEndpoint<Request, PagedListResponse<LinkedWorkflowDefinitionSummary>>
{
    public override void Configure()
    {
        Get("/workflow-definitions");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task<PagedListResponse<LinkedWorkflowDefinitionSummary>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.FromPage(request.Page, request.PageSize);
        var filter = CreateFilter(request);
        var summaries = await FindAsync(request, filter, pageArgs, cancellationToken);
        var pagedList = new PagedListResponse<WorkflowDefinitionSummary>(summaries);
        var response = linker.MapAsync(pagedList);
        return response;
    }

    private WorkflowDefinitionFilter CreateFilter(Request request)
    {
        var versionOptions = string.IsNullOrWhiteSpace(request.VersionOptions) ? default(VersionOptions?) : VersionOptions.FromString(request.VersionOptions);

        return new()
        {
            IsSystem = request.IsSystem,
            VersionOptions = versionOptions,
            SearchTerm = request.SearchTerm?.Trim(),
            MaterializerName = request.MaterializerName,
            DefinitionIds = request.DefinitionIds,
            Ids = request.Ids
        };
    }

    private async Task<Page<WorkflowDefinitionSummary>> FindAsync(Request request, WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken)
    {
        request.OrderBy ??= OrderByWorkflowDefinition.Name;

        var direction = request.OrderBy == OrderByWorkflowDefinition.Name ? request.OrderDirection ?? OrderDirection.Ascending : request.OrderDirection ?? OrderDirection.Descending;

        switch (request.OrderBy)
        {
            default:
                {
                    var order = new WorkflowDefinitionOrder<DateTimeOffset>
                    {
                        KeySelector = p => p.CreatedAt,
                        Direction = direction
                    };

                    return await store.FindSummariesAsync(filter, order, pageArgs, cancellationToken);
                }
            case OrderByWorkflowDefinition.Name:
                {
                    var order = new WorkflowDefinitionOrder<string>
                    {
                        KeySelector = p => p.Name!,
                        Direction = direction
                    };

                    return await store.FindSummariesAsync(filter, order, pageArgs, cancellationToken);
                }
            case OrderByWorkflowDefinition.Version:
                {
                    var order = new WorkflowDefinitionOrder<int>
                    {
                        KeySelector = p => p.Version,
                        Direction = direction
                    };

                    return await store.FindSummariesAsync(filter, order, pageArgs, cancellationToken);
                }
        }
    }
}