using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Swagger;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/workflow-definitions")]
    [Produces("application/json")]
    public class Post : ControllerBase
    {
        private readonly IWorkflowPublisher _workflowPublisher;

        public Post(IWorkflowPublisher workflowPublisher)
        {
            _workflowPublisher = workflowPublisher;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WorkflowDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionExample))]
        [SwaggerOperation(
            Summary = "Creates a new workflow definition or updates an existing one.",
            Description = "Creates a new workflow definition or updates an existing one. If the workflow already exists, a new draft is created and updated with the specified values. Use the Publish field to automatically publish the workflow.",
            OperationId = "WorkflowDefinitions.Post",
            Tags = new[] {"WorkflowDefinitions"})
        ]
        public async Task<ActionResult<WorkflowDefinition>> Handle(SaveWorkflowDefinitionRequest request, ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var workflowDefinition = await _workflowPublisher.GetDraftAsync(request.WorkflowDefinitionId, cancellationToken);

            if (workflowDefinition == null)
            {
                workflowDefinition = _workflowPublisher.New();

                if (!string.IsNullOrWhiteSpace(request.WorkflowDefinitionId))
                    workflowDefinition.WorkflowDefinitionId = request.WorkflowDefinitionId.Trim();
            }

            workflowDefinition.Activities = request.Activities;
            workflowDefinition.Connections = request.Connections;
            workflowDefinition.Description = request.Description?.Trim();
            workflowDefinition.Name = request.Name?.Trim();
            workflowDefinition.Variables = request.Variables;
            workflowDefinition.IsEnabled = request.Enabled;
            workflowDefinition.IsSingleton = request.IsSingleton;
            workflowDefinition.PersistenceBehavior = request.PersistenceBehavior;
            workflowDefinition.DeleteCompletedInstances = request.DeleteCompletedInstances;
            workflowDefinition.ContextOptions = request.ContextOptions;
            workflowDefinition.Type = "Workflow";
            workflowDefinition.ActivityId = workflowDefinition.WorkflowDefinitionId;
            workflowDefinition.DisplayName = request.DisplayName?.Trim();

            if (request.Publish)
                await _workflowPublisher.PublishAsync(workflowDefinition, cancellationToken);
            else
                await _workflowPublisher.SaveDraftAsync(workflowDefinition, cancellationToken);

            return CreatedAtAction("Handle", "Get", new {id = workflowDefinition.WorkflowDefinitionId, version = apiVersion.ToString()}, workflowDefinition);
        }
    }
}