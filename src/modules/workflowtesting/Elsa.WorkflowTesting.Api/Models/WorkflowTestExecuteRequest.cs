namespace Elsa.WorkflowTesting.Api.Models
{
    public class WorkflowTestExecuteRequest
    {
        public string WorkflowDefinitionId { get; init; } = default!;
        public int Version { get; init; }
        public string SignalRConnectionId { get; init; } = default!;
        public string? StartActivityId { get; init; } = default!;
    }
}