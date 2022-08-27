using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Common.Entities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Persistence.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.List;

public class List : Endpoint<Request, Response>
{
    private readonly IWorkflowInstanceStore _store;

    public List(IWorkflowInstanceStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/workflow-instances");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = new PageArgs(request.Page, request.PageSize);

        var args = new FindWorkflowInstancesArgs(
            request.SearchTerm,
            request.DefinitionId,
            request.Version,
            request.CorrelationId,
            request.Status,
            request.SubStatus,
            pageArgs,
            request.OrderBy ?? OrderBy.Created,
            request.OrderDirection ?? OrderDirection.Ascending);

        var summaries = await _store.FindManyAsync(args, cancellationToken);
        return new Response(summaries.Items, summaries.TotalCount);
    }
}