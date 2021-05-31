using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace SampleClientApp.Web.Controllers
{
    [ApiController]
    [Route("elsa-hook")]
    public class ElsaHookController : ControllerBase
    {
        [HttpPost("commands")]
        public IActionResult Command(ElsaCommand model)
        {
            Console.WriteLine("Received command {0} with payload {1} for workflow {2}", model.Command, model.Payload, model.WorkflowInstanceId);
            return Ok();
        }
        
        [HttpPost("tasks")]
        public IActionResult Task(ElsaTask model)
        {
            Console.WriteLine("Received instruction to run task {0} with payload {1} for workflow {2}", model.Task, model.Payload, model.WorkflowInstanceId);
            return Ok();
        }
    }

    public record ElsaCommand(string Command, JsonElement Payload, string WorkflowInstanceId);
    public record ElsaTask(string Task, JsonElement Payload, string WorkflowInstanceId);
}