using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowExecutionLogRecords;
using Elsa.Server.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Open.Linq.AsyncExtensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.ActivityStats
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{workflowInstanceId}/activity-stats/{activityId}")]
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ActivityStats))]
        [SwaggerOperation(
            Summary = "Returns an aggregated view of activity events and execution statistics.",
            Description = "Returns an aggregated view of activity events and execution statistics.",
            OperationId = "ActivityStats.Get",
            Tags = new[] { "ActivityStats" })
        ]
        public async Task<ActionResult<ActivityStats>> Handle(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowInstanceIdSpecification(workflowInstanceId).And(new ActivityIdSpecification(activityId));
            var orderBy = OrderBySpecification.OrderBy<WorkflowExecutionLogRecord>(x => x.Timestamp);
            var records = await _workflowExecutionLogStore.FindManyAsync(specification, orderBy, null, cancellationToken).ToList();
            var eventCounts = records.GroupBy(x => x.EventName).ToList();
            var executions = GetExecutions(records).ToList();
            var executionTimes = executions.Select(x => x.Duration).OrderBy(x => x).ToList();
            var faultRecord = records.FirstOrDefault(x => x.EventName == "Faulted");
            var activityFault = faultRecord != null ? new ActivityFault(faultRecord.Message!) : null;
            
            var model = new ActivityStats
            {
                EventCounts = eventCounts.Select(x => new ActivityEventCount(x.Key!, x.Count())).ToList(),
                LastExecutedAt = executions.Select(x => x.Timestamp).OrderByDescending(x => x).FirstOrDefault(),
                SlowestExecutionTime = executionTimes.LastOrDefault(),
                FastestExecutionTime = executionTimes.FirstOrDefault(),
                AverageExecutionTime = executionTimes.Any() ? Duration.FromTicks(executionTimes.Average(x => x.TotalTicks)) : default,
                Fault = activityFault
            };
            
            return Json(model, _serializerSettingsProvider.GetSettings());
        }

        private IEnumerable<ActivityExecutionStat> GetExecutions(List<WorkflowExecutionLogRecord> records)
        {
            var filteredRecords = records.Where(x => x.EventName is "Executing" or "Executed").OrderBy(x => x.Timestamp).ToList();

            if(!filteredRecords.Any())
                yield break;

            var index = 0;
            
            foreach (var @record in filteredRecords)
            {
                if (@record.EventName == "Executed")
                {
                    var previousRecord = filteredRecords.ElementAt(index - 1);
                    var duration = @record.Timestamp - previousRecord.Timestamp;
                    yield return new ActivityExecutionStat(@record.Timestamp, duration);
                }

                index++;
            }
        }
    }
}