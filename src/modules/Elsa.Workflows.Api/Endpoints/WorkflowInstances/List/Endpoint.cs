using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
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

        var direction = request.OrderDirection == OrderDirection.Ascending ? OrderDirection.Ascending : OrderDirection.Descending;
        var summaries = await FindAsync(request, filter, pageArgs, direction, cancellationToken);
        return new Response(summaries.Items, summaries.TotalCount);
    }

    private async Task<Page<WorkflowInstanceSummary>> FindAsync(Request request, WorkflowInstanceFilter filter, PageArgs pageArgs, OrderDirection direction, CancellationToken cancellationToken)
    {
        switch (request.OrderBy ?? OrderBy.Created)
        {
            default:
            case OrderBy.Created:
            {
                var o = new WorkflowInstanceOrder<DateTimeOffset>
                {
                    KeySelector = p => p.CreatedAt,
                    Direction = direction
                };

                return await _store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
            }
            case OrderBy.LastExecuted:
            {
                var o = new WorkflowInstanceOrder<DateTimeOffset?>
                {
                    KeySelector = p => p.LastExecutedAt,
                    Direction = direction
                };

                return await _store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
            }
            case OrderBy.Finished:
            {
                var o = new WorkflowInstanceOrder<DateTimeOffset?>
                {
                    KeySelector = p => p.FinishedAt,
                    Direction = direction
                };

                return await _store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
            }
        }
    }
}