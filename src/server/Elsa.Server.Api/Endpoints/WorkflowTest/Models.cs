namespace Elsa.Server.Api.Endpoints.WorkflowTest
{
    public sealed record WorkflowTestExecuteRequest
    {
        public string? WorkflowDefinitionId { get; init; }
        public int Version { get; init; }
        public string? SignalRConnectionId { get; init; }
    }

    public sealed record WorkflowTestSaveRequest
    {
        public string? WorkflowDefinitionId { get; init; }
        public int Version { get; init; }
        public string? ActivityId { get; init; }
        public object? Json { get; init; }
    }
}
