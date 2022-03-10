using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowExecutionLogRecords;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowExecutionLog
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{id}/execution-log")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IWorkflowExecutionLogStore _workflowExecutionLogStore;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public Get(IWorkflowExecutionLogStore workflowExecutionLogStore, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _workflowExecutionLogStore = workflowExecutionLogStore;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedList<WorkflowExecutionLogRecord>))]
        [SwaggerOperation(
            Summary = "Returns the workflow's execution log.",
            Description = "Returns the workflow's execution log.",
            OperationId = "WorkflowExecutionLog.Get",
            Tags = new[] { "WorkflowExecutionLog" })
        ]
        public async Task<IActionResult> Handle(string id, int? page = default, int? pageSize = default, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowInstanceIdSpecification(id);
            var totalCount = await _workflowExecutionLogStore.CountAsync(specification, cancellationToken);
            var paging = page != null ? Paging.Page(page.Value, pageSize ?? 100) : default;
            var orderBy = OrderBySpecification.OrderBy<WorkflowExecutionLogRecord>(x => x.Timestamp);
            var records = await _workflowExecutionLogStore.FindManyAsync(specification, orderBy, paging, cancellationToken).ToList();
            var model = new PagedList<WorkflowExecutionLogRecord>(records, page, pageSize, totalCount);

            return Json(model, _serializerSettingsProvider.GetSettings());
        }
    }
}