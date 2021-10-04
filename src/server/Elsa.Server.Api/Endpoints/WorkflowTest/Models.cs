namespace Elsa.Server.Api.Endpoints.WorkflowTest
{
    public sealed record WorkflowTestExecuteRequest
    {
        public string? WorkflowDefinitionId { get; init; }
        public int Version { get; init; }
        public string? SignalRConnectionId { get; init; }
    }
}
