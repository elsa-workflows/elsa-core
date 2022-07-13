namespace Elsa.WorkflowTesting.Api.Models
{
    public class WorkflowTestRestartFromActivityRequest : WorkflowTestExecuteRequest
    {
        public string ActivityId { get; init; } = default!;
        public string LastWorkflowInstanceId { get; init; } = default!;
    }
}