using System.Collections.Generic;
using System.Linq;
using Elsa.Server.Api.Services;
using Elsa.Services.WorkflowStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowStorageProviders
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/workflow-storage-providers")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IWorkflowStorageService _workflowStorageService;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public List(IWorkflowStorageService workflowStorageService, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _workflowStorageService = workflowStorageService;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WorkflowStorageDescriptor>))]
        [SwaggerOperation(
            Summary = "Returns all available workflow storage providers.",
            Description = "Returns all available workflow storage providers.",
            OperationId = "WorkflowStorageProviders.List",
            Tags = new[] { "WorkflowStorageProviders" })
        ]
        public IActionResult Handle()
        {
            var providers = _workflowStorageService.ListProviders();
            var descriptors = providers.Select(x => new WorkflowStorageDescriptor(x.Name, x.DisplayName)).ToList();
            return Json(descriptors, _serializerSettingsProvider.GetSettings());
        }
    }
}