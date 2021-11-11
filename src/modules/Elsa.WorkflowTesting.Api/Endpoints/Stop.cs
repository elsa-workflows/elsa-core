using Elsa.Server.Api.ActionFilters;
using Elsa.Testing.Api.Models;
using Elsa.Testing.Events;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Testing.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-test/stop")]
    [Produces("application/json")]
    public class Stop : Controller
    {
        private readonly IMediator _mediator;

        public Stop(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowTestStopRequest))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Stops the specified workflow definition execution in test mode.",
            Description = "Stops the specified workflow definition execution in test mode.",
            OperationId = "WorkflowTest.Stop",
            Tags = new[] { "WorkflowTest" })
        ]
        public async Task<IActionResult> Handle([FromBody] WorkflowTestStopRequest request, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WorkflowTestExecutionStopped(request.WorkflowInstanceId), cancellationToken);

            return Ok();
        }
    }
}
