using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Models
{
    public class WorkflowExecutionLogRecord : Entity, ITenantScope
    {
        public WorkflowExecutionLogRecord()
        {
        }

        public WorkflowExecutionLogRecord(string id, string? tenantId, string workflowInstanceId, string activityId, Instant timestamp, string? message, JObject? data = default)
        {
            Id = id;
            TenantId = tenantId;
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            Timestamp = timestamp;
            Message = message;
            Data = data;
        }
        
        public string? TenantId { get; private set; }
        public string WorkflowInstanceId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public Instant Timestamp { get; set; }= default!;
        public string? Message { get; set; }
        public JObject? Data { get; set; }
    }
}