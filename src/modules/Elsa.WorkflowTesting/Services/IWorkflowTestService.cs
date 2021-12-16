using Elsa.WorkflowTesting.Messages;
using System.Threading.Tasks;

namespace Elsa.WorkflowTesting.Services
{
    public interface IWorkflowTestService
    {
        Task DispatchMessage(string signalRConnectionId, WorkflowTestMessage message);
    }
}
