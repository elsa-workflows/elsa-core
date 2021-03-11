﻿using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{workflowDefinitionId}/{versionOptions}/export")]
    [Produces("application/json")]
    public class Export : ControllerBase
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IContentSerializer _contentSerializer;

        public Export(IWorkflowDefinitionStore workflowDefinitionStore, IContentSerializer contentSerializer)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _contentSerializer = contentSerializer;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Exports the specified workflow definition as a JSON download.",
            Description = "Exports the specified workflow definition as a JSON download.",
            OperationId = "WorkflowDefinitions.Export",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken)
        {
            var workflowDefinition = await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, versionOptions, cancellationToken);

            if (workflowDefinition == null)
                return NotFound();

            var json = _contentSerializer.Serialize(workflowDefinition);
            var bytes = Encoding.UTF8.GetBytes(json);
            var workflowName = workflowDefinition.Name is null or "" ? workflowDefinition.DefinitionId : workflowDefinition.Name.Trim();
            var fileName = $"workflow-definition-{workflowName}.json";
            
            return File(bytes, "application/json", fileName);
        }
    }
}