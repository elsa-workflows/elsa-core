using Newtonsoft.Json.Linq;

namespace Elsa.Server.Api.Models
{
    public class WorkflowTestMessage
    {
        public string SignalRConnectionId { get; set; } = default!;
        public string WorkflowInstanceId { get; set; } = default!;
        public string CorrelationId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string Status { get; set; } = default!;
        public object? Data { get; set; }
    }
}
