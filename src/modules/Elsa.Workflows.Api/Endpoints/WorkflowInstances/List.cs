using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore.Attributes;
using Elsa.Models;
using Elsa.Persistence.Common.Entities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowInstances, "List")]
public class List : Controller
{
    private readonly IWorkflowInstanceStore _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public List(IWorkflowInstanceStore store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }
    
    [HttpGet]
    public async Task<IActionResult> HandleAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? searchTerm,
        [FromQuery] string? definitionId,
        [FromQuery] string? correlationId,
        [FromQuery] int? version,
        [FromQuery] WorkflowStatus? status,
        [FromQuery] WorkflowSubStatus? subStatus,
        [FromQuery] OrderBy? orderBy,
        [FromQuery] OrderDirection? orderDirection,
        CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var pageArgs = new PageArgs(page, pageSize);

        var request = new FindWorkflowInstancesArgs(
            searchTerm,
            definitionId,
            version,
            correlationId,
            status,
            subStatus,
            pageArgs,
            orderBy ?? OrderBy.Created,
            orderDirection ?? OrderDirection.Ascending);

        var summaries = await _store.FindManyAsync(request, cancellationToken);

        return Json(summaries, serializerOptions);
    }
}