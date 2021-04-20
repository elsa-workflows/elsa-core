using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Models
{
    public class WorkflowExecutionLogRecord : Entity, ITenantScope
    {
        public WorkflowExecutionLogRecord()
        {
        }

        public WorkflowExecutionLogRecord(string id, string? tenantId, string workflowInstanceId, string activityId, string activityType, Instant timestamp, string eventName, string? message, string? source = default, JObject? data = default)
        {
            Id = id;
            TenantId = tenantId;
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            ActivityType = activityType;
            Timestamp = timestamp;
            EventName = eventName;
            Message = message;
            Source = source;
            Data = data;
        }

        public string? TenantId { get; private set; }
        public string WorkflowInstanceId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public Instant Timestamp { get; set; } = default!;
        public string? EventName { get; set; }
        public string? Message { get; set; }
        public string? Source { get; set; }
        public JObject? Data { get; set; }
    }
}