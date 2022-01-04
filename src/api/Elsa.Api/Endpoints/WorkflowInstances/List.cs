using System.Threading;
using System.Threading.Tasks;
using Elsa.Management.Serialization;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowInstances;

public static partial class WorkflowInstances
{
    public static async Task<IResult> ListAsync(
        IRequestSender requestSender,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        CancellationToken cancellationToken,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? searchTerm,
        [FromQuery] string? definitionId,
        [FromQuery] string? correlationId,
        [FromQuery] int? version,
        [FromQuery] WorkflowStatus? workflowStatus,
        [FromQuery] OrderBy? orderBy,
        [FromQuery] OrderDirection? orderDirection)
    {
        var serializerOptions = serializerOptionsProvider.CreateSerializerOptions();
        var skip = page * pageSize;
        var take = pageSize;
        var request = new ListWorkflowInstanceSummaries(searchTerm, definitionId, version, correlationId, workflowStatus, orderBy ?? OrderBy.Created, orderDirection ?? OrderDirection.Ascending, skip ?? 0, take ?? 50);
        var summaries = await requestSender.RequestAsync(request, cancellationToken);

        return Results.Json(summaries, serializerOptions, statusCode: StatusCodes.Status200OK);
    }
}