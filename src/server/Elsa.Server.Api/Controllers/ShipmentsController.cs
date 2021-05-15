using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Server.Api.Controllers
{
    [ApiController]
    [Route("api/shipments")]
    public class ShipmentsController : Controller
    {
        private readonly ISignaler _signaler;

        public ShipmentsController(ISignaler signaler)
        {
            _signaler = signaler;
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateShipmentForCustomer([FromQuery]string customerId, CancellationToken cancellationToken)
        {
            await _signaler.TriggerSignalAsync("ship-order", correlationId: customerId, cancellationToken: cancellationToken);
            return new EmptyResult();
        }
    }
}