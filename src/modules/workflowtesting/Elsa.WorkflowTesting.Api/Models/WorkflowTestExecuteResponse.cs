namespace Elsa.WorkflowTesting.Api.Models
{
    public class WorkflowTestExecuteResponse
    {
        public bool IsSuccess { get; set; }
        public bool IsAnotherInstanceRunning { get; set; }
    }
}
