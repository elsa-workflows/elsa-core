using System.Collections.Generic;
using System.Linq;
using Elsa.Providers.Workflows;
using Elsa.Server.Api.Services;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowProviders
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/workflow-providers")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IEnumerable<IWorkflowProvider> _workflowProviders;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public List(IEnumerable<IWorkflowProvider> workflowProviders, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _workflowProviders = workflowProviders;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WorkflowProviderDescriptor>))]
        [SwaggerOperation(
            Summary = "Returns all available workflow providers.",
            Description = "Returns all available workflow providers.",
            OperationId = "WorkflowProviders.List",
            Tags = new[] { "WorkflowProviders" })
        ]
        public IActionResult Handle()
        {
            var providers = _workflowProviders.ToList();
            var descriptors = providers.Select(x => new WorkflowProviderDescriptor(x.GetType().Name, x.GetType().Name.Humanize())).ToList();
            return Json(descriptors, _serializerSettingsProvider.GetSettings());
        }
    }
}