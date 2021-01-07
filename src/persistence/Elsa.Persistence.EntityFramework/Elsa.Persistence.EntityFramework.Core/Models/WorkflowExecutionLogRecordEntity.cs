using Elsa.Models;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Persistence.EntityFramework.Core.Models
{
    public class WorkflowExecutionLogRecordEntity : IEntity
    {
        public string Id { get; set; } = default!;
        public string? TenantId { get; set; }
        public string WorkflowInstanceId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public Instant Timestamp { get; set; }= default!;
        public string? Message { get; set; }
        public JObject? Data { get; set; }
    }
}