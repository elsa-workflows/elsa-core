using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.Labels;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.Labels, "List")]
[ProducesResponseType(typeof(Page<Label>), StatusCodes.Status200OK)]
public class List : Controller
{
    private readonly ILabelStore _store;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public List(ILabelStore store, WorkflowSerializerOptionsProvider serializerOptionsProvider)
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