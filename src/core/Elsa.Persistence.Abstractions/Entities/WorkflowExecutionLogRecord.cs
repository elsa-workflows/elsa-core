namespace Elsa.Persistence.Entities
{
    public class WorkflowExecutionLogRecord : Entity
    {
        public string WorkflowDefinitionId { get; set; } = default!;
        public string WorkflowInstanceId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public DateTimeOffset Timestamp { get; set; }
        public string? EventName { get; set; }
        public string? Message { get; set; }
        public string? Source { get; set; }
        public object? Payload { get; set; }
    }
}