using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Persistence.YesSql.Documents
{
    public class WorkflowExecutionLogRecordDocument : YesSqlDocument
    {
        public string RecordId { get; set; } = default!;
        public string? TenantId { get; set; }
        public string WorkflowInstanceId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public Instant Timestamp { get; set; }
        public string EventName { get; set; } = default!;
        public string? Message { get; set; }
        public string? Source { get; set; }
        public JObject? Data { get; set; }
    }
}