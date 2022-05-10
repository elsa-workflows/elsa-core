using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "List")]
[ProducesResponseType(typeof(Page<WorkflowDefinitionSummary>), StatusCodes.Status200OK)]
public class List : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public List(IWorkflowDefinitionStore store, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public async Task<IActionResult> HandleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? versionOptions = default,
        [FromQuery] string? definitionIds = default,
        [FromQuery(Name = "materializer")] string? materializerName = default,
        [FromQuery] int? page = default,
        [FromQuery] int? pageSize = default)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var pageArgs = new PageArgs(page, pageSize);
        var parsedVersionOptions = versionOptions != null ? VersionOptions.FromString(versionOptions) : default;
        var splitIds = definitionIds?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        if (splitIds.Any())
        {
            var summaries = await _store.FindManySummariesAsync(splitIds, parsedVersionOptions, cancellationToken).ToList();
            var pageOfSummaries = new Page<WorkflowDefinitionSummary>(summaries, summaries.Count);

            return Json(pageOfSummaries, serializerOptions);
        }
        else
        {
            var pageOfSummaries = await _store.ListSummariesAsync(parsedVersionOptions, materializerName, pageArgs, cancellationToken);
            return Json(pageOfSummaries, serializerOptions);
        }
    }
}