using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace SampleClientApp.Web.Controllers
{
    [ApiController]
    [Route("elsa-hook")]
    public class ElsaHookController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post(ElsaCommand model)
        {
            Console.WriteLine("Received command {0} with payload {1} for workflow {2}", model.Command, model.Payload, model.WorkflowInstanceId);
            return Ok();
        }
    }

    public record ElsaCommand(string Command, JsonElement Payload, string WorkflowInstanceId);
}