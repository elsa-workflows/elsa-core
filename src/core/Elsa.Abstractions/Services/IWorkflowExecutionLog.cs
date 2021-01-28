using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public interface IWorkflowExecutionLog
    {
        Task AddEntryAsync(string message, string workflowInstanceId, string activityId, string activityType, string? tenantId, string? source, JObject? data, CancellationToken cancellationToken);
    }
}