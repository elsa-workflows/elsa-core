using MassTransit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Elsa.Samples.MassTransitRabbitMq.Messages;

namespace Elsa.Samples.MassTransitRabbitMq.Controllers
{
    [ApiController]
    [Route("trigger-message")]
    public class TriggerMessageController : Controller
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public TriggerMessageController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [Route("first")]
        [HttpPost]
        public async Task<IActionResult> TriggerFirstMessage()
        {
            var message = new FirstMessage();
            await _publishEndpoint.Publish(message);
            return Ok();
        }

        [Route("second")]
        [HttpPost]
        public async Task<IActionResult> TriggerSecondMessage()
        {
            var message = new SecondMessage();
            await _publishEndpoint.Publish(message);
            return Ok();
        }
    }
}