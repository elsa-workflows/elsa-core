using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.Labels;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.Labels, "Get")]
[ProducesResponseType(typeof(Label), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class Get : Controller
{
    private readonly ILabelStore _store;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public Get(ILabelStore store, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpGet]
    public async Task<IActionResult> HandleAsync(string id, CancellationToken cancellationToken)
    {
        var label = await _store.FindByIdAsync(id, cancellationToken);
        return label == null ? NotFound() : Json(label, _serializerOptionsProvider.CreateApiOptions());
    }
}