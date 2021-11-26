namespace Elsa.WorkflowTesting.Api.Models
{
    public class WorkflowTestExecuteRequest
    {
        public string? WorkflowDefinitionId { get; init; }
        public int Version { get; init; }
        public string? SignalRConnectionId { get; init; }
    }
}