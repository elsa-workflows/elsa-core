using System.Threading.Tasks;
using Elsa.WorkflowTesting.Api.Hubs;
using Elsa.WorkflowTesting.Messages;
using Elsa.WorkflowTesting.Services;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.WorkflowTesting.Api.Services
{
    public class WorkflowTestService : Hub, IWorkflowTestService
    {
        private readonly IHubContext<WorkflowTestHub> _hubContext;

        public WorkflowTestService(IHubContext<WorkflowTestHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task DispatchMessage(string signalRConnectionId, WorkflowTestMessage message)
        {
            await _hubContext.Clients.Client(signalRConnectionId).SendAsync("DispatchMessage", message);
        }
    }
}
