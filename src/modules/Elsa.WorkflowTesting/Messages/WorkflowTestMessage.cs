using System.Collections.Generic;

namespace Elsa.WorkflowTesting.Messages
{
    public class WorkflowTestMessage
    {
        public string ActivityType { get; set; } = default!;
        public string WorkflowInstanceId { get; set; } = default!;
        public string CorrelationId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string WorkflowStatus { get; set; } = default!;
        public object? Data { get; set; }
        public string? Error { get; set; }
        public IDictionary<string, object?>? ActivityData { get; set; }
    }
}
