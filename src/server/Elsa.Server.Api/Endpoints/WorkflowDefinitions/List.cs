using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Server.Api.Models;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/workflow-definitions")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IContentSerializer _serializer;

        public List(IWorkflowDefinitionManager workflowDefinitionManager, IContentSerializer serializer)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
            _serializer = serializer;
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<WorkflowDefinition>>> Handle(int page = 0, int pageSize = 50, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            version ??= VersionOptions.Latest;
            var totalCount = await _workflowDefinitionManager.CountAsync(version, cancellationToken);
            var items = await _workflowDefinitionManager.ListAsync(page, pageSize, version, cancellationToken);
            var pagedList = new PagedList<WorkflowDefinition>(items, page, pageSize, totalCount);

            return Json(pagedList, _serializer.GetSettings());
        }
    }
}