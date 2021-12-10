using Elsa.WorkflowTesting.Messages;

namespace Elsa.WorkflowTesting.Services
{
    public interface IWorkflowTestService
    {
        Task DispatchMessage(string signalRConnectionId, WorkflowTestMessage message);
    }
}
