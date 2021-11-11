using Elsa.WorkflowTesting.Models;

namespace Elsa.WorkflowTesting.Services
{
    public interface IWorkflowTestService
    {
        Task DispatchMessage(string signalRConnectionId, WorkflowTestMessage message);
    }
}
