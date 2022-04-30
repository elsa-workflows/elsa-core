using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowInstances;

public static partial class WorkflowInstances
{
    public static async Task<IResult> ListAsync(
        IWorkflowInstanceStore workflowInstanceStore,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        CancellationToken cancellationToken,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? searchTerm,
        [FromQuery] string? definitionId,
        [FromQuery] string? correlationId,
        [FromQuery] int? version,
        [FromQuery] WorkflowStatus? workflowStatus,
        [FromQuery] WorkflowSubStatus? workflowSubStatus,
        [FromQuery] OrderBy? orderBy,
        [FromQuery] OrderDirection? orderDirection)
    {
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var pageArgs = new PageArgs(page, pageSize);

        var request = new FindWorkflowInstancesArgs(
            searchTerm,
            definitionId,
            version,
            correlationId,
            workflowStatus,
            workflowSubStatus,
            pageArgs,
            orderBy ?? OrderBy.Created,
            orderDirection ?? OrderDirection.Ascending);

        var summaries = await workflowInstanceStore.FindManyAsync(request, cancellationToken);

        return Results.Json(summaries, serializerOptions, statusCode: StatusCodes.Status200OK);
    }
}