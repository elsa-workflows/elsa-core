using Elsa.AspNetCore;
using Elsa.AspNetCore.Attributes;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Labels.Endpoints.Labels;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.Labels, "Delete")]
[ProducesResponseType(typeof(Label), StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class Delete : Controller
{
    private readonly ILabelStore _store;

    public Delete(ILabelStore store) => _store = store;

    [HttpDelete]
    public async Task<IActionResult> HandleAsync(string id, CancellationToken cancellationToken)
    {
        var deleted = await _store.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}