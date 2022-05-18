using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.Labels;

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