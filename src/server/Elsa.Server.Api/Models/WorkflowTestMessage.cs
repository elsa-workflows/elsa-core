namespace Elsa.Server.Api.Models
{
    public class WorkflowTestMessage
    {
        public string CorrelationId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string Status { get; set; } = default!;
        public object? Data { get; set; }
        public string? Error { get; set; }
    }
}
