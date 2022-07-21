using Elsa.AspNetCore.Attributes;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Labels.Endpoints.Labels;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.Labels, "List")]
[ProducesResponseType(typeof(Page<Label>), StatusCodes.Status200OK)]
public class List : Controller
{
    private readonly ILabelStore _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public List(ILabelStore store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpGet]
    public async Task<IActionResult> HandleAsync([FromQuery] PageArgs? pageArgs, CancellationToken cancellationToken = default)
    {
        var pageOfLabels = await _store.ListAsync(pageArgs, cancellationToken);
        return Json(pageOfLabels, _serializerOptionsProvider.CreateApiOptions());
    }
}