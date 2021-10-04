using System.Threading.Tasks;
using Elsa.Server.Api.Models;

namespace Elsa.Server.Api.Services
{
    public interface IWorkflowTestService
    {
        Task DispatchMessage(string signalRConnectionId, WorkflowTestMessage message);
    }
}
