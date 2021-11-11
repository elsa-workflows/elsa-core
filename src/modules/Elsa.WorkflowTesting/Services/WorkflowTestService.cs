using Elsa.WorkflowTesting.Hubs;
using Elsa.WorkflowTesting.Models;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.WorkflowTesting.Services
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
