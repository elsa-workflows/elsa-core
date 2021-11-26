namespace Elsa.WorkflowTesting.Api.Models
{
    public class WorkflowTestRestartFromActivityRequest : WorkflowTestExecuteRequest
    {
        public string ActivityId { get; init; }
        public string LastWorkflowInstanceId { get; init; }
    }
}