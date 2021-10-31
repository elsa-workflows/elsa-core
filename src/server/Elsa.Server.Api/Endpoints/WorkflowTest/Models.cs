namespace Elsa.Server.Api.Endpoints.WorkflowTest
{
    public sealed record WorkflowTestExecuteRequest
    {
        public string? WorkflowDefinitionId { get; init; }
        public int Version { get; init; }
        public string? SignalRConnectionId { get; init; }
    }

    public sealed record WorkflowTestRestartFromActivityRequest
    {
        public string WorkflowDefinitionId { get; init; }
        public int Version { get; init; }
        public string ActivityId { get; init; }
        public string LastWorkflowInstanceId { get; init; }
        public string SignalRConnectionId { get; init; }
    }
}
