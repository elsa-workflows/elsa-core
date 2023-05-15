using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.List;

[PublicAPI]
internal class List : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowInstanceStore _store;

    public List(IWorkflowInstanceStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/workflow-instances");
        ConfigurePermissions("read:workflow-instances");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = new PageArgs(request.Page, request.PageSize);

        var filter = new WorkflowInstanceFilter
        {
            SearchTerm = request.SearchTerm,
            DefinitionId = request.DefinitionId,
            Version = request.Version,
            CorrelationId = request.CorrelationId,
            WorkflowStatus = request.Status,
            WorkflowSubStatus = request.SubStatus
        };

        var summaries = await FindAsync(request, filter, pageArgs, cancellationToken);
        return new Response(summaries.Items, summaries.TotalCount);
    }

    private async Task<Page<WorkflowInstanceSummary>> FindAsync(Request request, WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken)
    {
        request.OrderBy = request.OrderBy ?? OrderByWfInstance.Created;
        var direction = request.OrderBy == OrderByWfInstance.Name ? (request.OrderDirection ?? OrderDirection.Ascending) : (request.OrderDirection ?? OrderDirection.Descending);

        switch (request.OrderBy)
        {
            default:
            case OrderByWfInstance.Created:
                {
                    var o = new WorkflowInstanceOrder<DateTimeOffset>
                    {
                        KeySelector = p => p.CreatedAt,
                        Direction = direction
                    };

                    return await _store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
                }
            case OrderByWfInstance.LastExecuted:
                {
                    var o = new WorkflowInstanceOrder<DateTimeOffset?>
                    {
                        KeySelector = p => p.LastExecutedAt,
                        Direction = direction
                    };

                    return await _store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
                }
            case OrderByWfInstance.Finished:
                {
                    var o = new WorkflowInstanceOrder<DateTimeOffset?>
                    {
                        KeySelector = p => p.FinishedAt,
                        Direction = direction
                    };

                    return await _store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
                }
            case OrderByWfInstance.Name:
                {
                    var o = new WorkflowInstanceOrder<string>
                    {
                        KeySelector = p => p.Name,
                        Direction = direction
                    };

                    return await _store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
                }
        }
    }
}