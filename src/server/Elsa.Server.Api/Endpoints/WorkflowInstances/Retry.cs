﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{id}/retry")]
    [Produces("application/json")]
    public class Retry : Controller
    {
        private readonly IWorkflowInstanceStore _store;
        private readonly IWorkflowReviver _reviver;

        public Retry(IWorkflowInstanceStore store, IWorkflowReviver reviver)
        {
            _store = store;
            _reviver = reviver;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowInstance))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(
            Summary = "Retries a faulted workflow instance.",
            Description = "Retries a workflow instance.",
            OperationId = "WorkflowInstances.Retry",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(string id, RevivalOptions? options, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _store.FindByIdAsync(id, cancellationToken);

            if (workflowInstance == null)
                return NotFound();

            workflowInstance = options?.RunImmediately == true 
                ? await _reviver.ReviveAndRunAsync(workflowInstance, cancellationToken) 
                : await _reviver.ReviveAndQueueAsync(workflowInstance, cancellationToken);

            return Response.HasStarted ? (IActionResult) new EmptyResult() : Ok(workflowInstance);
        }
    }

    public record RevivalOptions(bool RunImmediately)
    {
        /// <summary>
        /// Set to true to run the revived workflow immediately, set to false to enqueue the revived workflow for execution.
        /// </summary>
        public bool RunImmediately { get; set; } = RunImmediately;
    }
}