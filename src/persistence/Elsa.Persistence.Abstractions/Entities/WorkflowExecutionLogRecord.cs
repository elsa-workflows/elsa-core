namespace Elsa.Persistence.Entities
{
    public class WorkflowExecutionLogRecord : Entity
    {
        public string WorkflowInstanceId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public DateTime Timestamp { get; set; } = default!;
        public string? EventName { get; set; }
        public string? Message { get; set; }
        public string? Source { get; set; }
        public object? Payload { get; set; }
    }
}