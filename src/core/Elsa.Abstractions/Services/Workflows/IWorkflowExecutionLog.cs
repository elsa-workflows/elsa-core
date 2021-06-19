using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public interface IWorkflowExecutionLog
    {
        Task AddEntryAsync(string workflowInstanceId, string activityId, string activityType, string eventName, string? message, string? tenantId, string? source, JObject? data, CancellationToken cancellationToken = default);
        Task<WorkflowExecutionLogRecord?> FindEntryAsync(ISpecification<WorkflowExecutionLogRecord> specification, CancellationToken cancellationToken = default);
    }
}