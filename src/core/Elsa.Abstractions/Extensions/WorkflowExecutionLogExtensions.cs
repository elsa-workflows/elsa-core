using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa
{
    public static class WorkflowExecutionLogExtensions
    {
        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string message, WorkflowInstance workflowInstance, IActivityBlueprint activity, CancellationToken cancellationToken = default) =>
            await log.AddEntryAsync(message, workflowInstance, activity, default, cancellationToken);

        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string message, WorkflowInstance workflowInstance, IActivityBlueprint activity, JObject? data, CancellationToken cancellationToken = default)
        {
            var tenantId = workflowInstance.TenantId;
            var workflowInstanceId = workflowInstance.Id;
            var activityId = activity.Id;
            var activityType = activity.Type;
            var source = activity.Source;

            await log.AddEntryAsync(message, workflowInstanceId, activityId, activityType, tenantId, source, data, cancellationToken);
        }
    }
}