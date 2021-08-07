using System.Collections.Generic;
using System.Linq;
using Elsa.Server.Api.ActionFilters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowChannels
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-channels")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IList<string> _workflowChannels;
        public List(ElsaOptions elsaOptions) => _workflowChannels = elsaOptions.WorkflowChannelOptions.Channels.ToList();

        [HttpGet]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerOperation(
            Summary = "Returns a list of available workflow channels.",
            Description = "Returns a list of available workflow channels.",
            OperationId = "WorkflowChannels.List",
            Tags = new[] { "WorkflowChannels" })
        ]
        public IActionResult Handle() => Ok(_workflowChannels);
    }
}