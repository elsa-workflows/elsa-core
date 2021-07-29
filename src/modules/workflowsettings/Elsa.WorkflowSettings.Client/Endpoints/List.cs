using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Services;
using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Client.Swagger.Examples;
using Elsa.WorkflowSettings.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.WorkflowSettings.Client.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-settings")]
    [Produces(MediaTypeNames.Application.Json)]
    public class List : Controller
    {
        private readonly IWorkflowSettingsStore _workflowSettingsStore;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public List(IWorkflowSettingsStore store, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _workflowSettingsStore = store;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<WorkflowSetting>))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowSettingsListExample))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow settings.",
            Description = "Returns a list of workflow settings.",
            OperationId = "WorkflowSettings.List",
            Tags = new[] { "WorkflowSettings" })
        ]
        public async Task<ActionResult<PagedList<WorkflowSetting>>> Handle(CancellationToken cancellationToken = default)
        {
            var specification = Specification<WorkflowSetting>.Identity;
            var items = await _workflowSettingsStore.FindManyAsync(specification, cancellationToken: cancellationToken);

            return Json(items, _serializerSettingsProvider.GetSettings());
        }
    }
}