using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.Common.Models;
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
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public List(IWorkflowInstanceStore store, WorkflowSerializerOptionsProvider serializerOptionsProvider)
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
        [FromQuery] WorkflowStatus? workflowStatus,
        [FromQuery] WorkflowSubStatus? workflowSubStatus,
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
            workflowStatus,
            workflowSubStatus,
            pageArgs,
            orderBy ?? OrderBy.Created,
            orderDirection ?? OrderDirection.Ascending);

        var summaries = await _store.FindManyAsync(request, cancellationToken);

        return Json(summaries, serializerOptions);
    }
}