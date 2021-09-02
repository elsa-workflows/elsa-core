using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Services;
using Elsa.WorkflowSettings.Api.Swagger.Examples;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.WorkflowSettings.Api.Endpoints
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
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowSettingListExample))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow setting.",
            Description = "Returns a list of workflow setting.",
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