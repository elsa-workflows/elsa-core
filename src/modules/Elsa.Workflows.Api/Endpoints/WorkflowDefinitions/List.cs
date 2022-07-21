using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore.Attributes;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitions, "List")]
[ProducesResponseType(typeof(Page<WorkflowDefinitionSummary>), StatusCodes.Status200OK)]
public class List : Controller
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public List(IWorkflowDefinitionStore store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public async Task<IActionResult> HandleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? versionOptions = default,
        [FromQuery] string? definitionIds = default,
        [FromQuery(Name = "materializer")] string? materializerName = default,
        //[FromQuery(Name = "label")] string[]? labels = default,
        [FromQuery] int? page = default,
        [FromQuery] int? pageSize = default)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var pageArgs = new PageArgs(page, pageSize);
        var parsedVersionOptions = versionOptions != null ? VersionOptions.FromString(versionOptions) : default(VersionOptions?);
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